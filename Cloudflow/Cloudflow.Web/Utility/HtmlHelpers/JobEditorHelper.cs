﻿using Cloudflow.Core.Extensions;
using Cloudflow.Core.Extensions.ExtensionAttributes;
using Cloudflow.Web.ViewModels.ExtensionConfigurationEdits;
using Cloudflow.Web.ViewModels.Jobs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Cloudflow.Web.Utility.HtmlHelpers
{
    public static class JobEditorHelper
    {
        #region Enums
        private enum PropertyTypes
        {
            Hidden,
            Text,
            Number,
            Collection,
            Complex,
            Unknown
        }

        private enum InputTypes
        {
            Hidden,
            Numeric,
            Text
        }
        #endregion

        #region Private Members
        private static readonly log4net.ILog _log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static HashSet<Type> _textTypes = new HashSet<Type>
        {
            typeof(string),
            typeof(Guid)
        };

        private static HashSet<Type> _numericTypes = new HashSet<Type>
        {
            typeof(byte),
            typeof(sbyte),
            typeof(UInt16),
            typeof(UInt32),
            typeof(UInt64),
            typeof(Int16),
            typeof(Int32),
            typeof(Int64),
            typeof(decimal),
            typeof(double),
            typeof(Single),
        };
        #endregion

        #region Private Methods
        private static ResourceManager LoadResources(Type type)
        {
            var defaultResources = type.Assembly.GetManifestResourceNames().FirstOrDefault(i => i.Contains("Properties.Resources"));

            if (defaultResources != null)
            {
                var resourceBaseName = defaultResources.Remove(defaultResources.LastIndexOf("."));
                return new ResourceManager(resourceBaseName, type.Assembly);
            }
            return null;
        }

        private static PropertyInfo[] GetSortedProperties(this Type type)
        {
            return type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).
                Select(i => new
                {
                    Property = i,
                    Attribute = (DisplayOrderAttribute)Attribute.GetCustomAttribute(i, typeof(DisplayOrderAttribute), true)
                })
                .OrderBy(i => i.Attribute != null ? i.Attribute.Order : 0)
                .Select(i => i.Property)
                .ToArray();
        }

        private static bool IsTextType(this Type type)
        {
            //Check to see if the type is text or if it's nullable text
            return _textTypes.Contains(type) ||
                   _textTypes.Contains(Nullable.GetUnderlyingType(type));
        }

        private static bool IsNumericType(this Type type)
        {
            //Check to see if the type is numeric or if it's a nullable numeric
            return _numericTypes.Contains(type) ||
                   _numericTypes.Contains(Nullable.GetUnderlyingType(type));
        }

        private static bool IsCollection(this Type type)
        {
            return typeof(IEnumerable).IsAssignableFrom(type) ||
                type.GetInterface(typeof(IEnumerable<>).FullName) != null;
        }

        private static PropertyTypes GetPropertyType(PropertyInfo propertyInfo)
        {
            if (Attribute.IsDefined(propertyInfo, typeof(HiddenAttribute)))
            {
                return PropertyTypes.Hidden;
            }

            if (propertyInfo.PropertyType.IsTextType())
            {
                return PropertyTypes.Text;
            }

            if (propertyInfo.PropertyType.IsNumericType())
            {
                return PropertyTypes.Number;
            }

            if (propertyInfo.PropertyType.IsCollection())
            {
                return PropertyTypes.Collection;
            }

            //If the property type has properties itself, we can consider it a complex type
            if (propertyInfo.PropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Any())
            {
                return PropertyTypes.Complex;
            }

            //Otherwise we don't know what to do with it at this point
            return PropertyTypes.Unknown;
        }

        private static PropertyTypes GetCollectionItemType(PropertyInfo propertyInfo)
        {
            var listType = propertyInfo.PropertyType.GetGenericArguments().Single();

            if (listType.IsTextType())
            {
                return PropertyTypes.Text;
            }

            if (listType.IsNumericType())
            {
                return PropertyTypes.Number;
            }

            if (listType.IsCollection())
            {
                return PropertyTypes.Collection;
            }

            //If the property type has properties itself, we can consider it a complex type
            if (listType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Any())
            {
                return PropertyTypes.Complex;
            }

            //Otherwise we don't know what to do with it at this point
            return PropertyTypes.Unknown;
        }

        private static string GetView(HtmlHelper htmlHelper, string name, object model)
        {
            htmlHelper.ViewContext.Controller.ViewData.Model = model;
            var result = ViewEngines.Engines.FindPartialView(htmlHelper.ViewContext.Controller.ControllerContext, name);
            using (var writer = new StringWriter())
            {
                var viewContext = new ViewContext(htmlHelper.ViewContext.Controller.ControllerContext, result.View,
                    htmlHelper.ViewContext.Controller.ViewData, htmlHelper.ViewContext.Controller.TempData, writer);

                result.View.Render(viewContext, writer);
                var html = writer.GetStringBuilder().ToString();
                return html;
            }
        }

        private static string GetLabelText(PropertyInfo propertyInfo, ResourceManager resourceManager)
        {
            var labelTextAttribute = (LabelTextResourceAttribute)propertyInfo.GetCustomAttribute(typeof(LabelTextResourceAttribute));
            if (labelTextAttribute != null && resourceManager != null)
            {
                return resourceManager.GetString(labelTextAttribute.ResourceName);
            }
            else
            {
                return propertyInfo.Name;
            }
        }

        private static string Label(List<string> propertyNameParts, PropertyInfo propertyInfo, ResourceManager resourceManager)
        {
            StringBuilder htmlStringBuilder = new StringBuilder();

            var tagBuilder = new TagBuilder("label");
            var name = string.Join(".", propertyNameParts);
            tagBuilder.MergeAttribute("for", name);

            tagBuilder.SetInnerText(GetLabelText(propertyInfo, resourceManager));

            htmlStringBuilder.AppendLine(tagBuilder.ToString(TagRenderMode.Normal));

            return htmlStringBuilder.ToString();
        }

        private static string Input(List<string> propertyNameParts, string value, InputTypes inputType)
        {
            var tagBuilder = new TagBuilder("input");

            var id = string.Join("_", propertyNameParts);
            tagBuilder.MergeAttribute("id", id);

            var name = string.Join(".", propertyNameParts);
            tagBuilder.MergeAttribute("name", name);

            switch (inputType)
            {
                case InputTypes.Hidden:
                    tagBuilder.MergeAttribute("type", "hidden");
                    break;
                case InputTypes.Numeric:
                    tagBuilder.MergeAttribute("type", "number");
                    break;
                case InputTypes.Text:
                    tagBuilder.MergeAttribute("type", "text");
                    break;
            }

            tagBuilder.MergeAttribute("value", value);

            tagBuilder.AddCssClass("form-control");

            return tagBuilder.ToString(TagRenderMode.SelfClosing);
        }

        private static string Editor(HtmlHelper htmlHelper, object model, List<string> propertyNameParts)
        {
            StringBuilder htmlStringBuilder = new StringBuilder();

            var resourceManager = LoadResources(model.GetType());

            foreach (var propertyInfo in model.GetType().GetSortedProperties())
            {
                var thisPropertyNameParts = new List<string>();
                thisPropertyNameParts.AddRange(propertyNameParts);
                thisPropertyNameParts.Add(propertyInfo.Name);
                
                switch (GetPropertyType(propertyInfo))
                {
                    case PropertyTypes.Hidden:
                        htmlStringBuilder.AppendLine(Input(thisPropertyNameParts, propertyInfo.GetValue(model)?.ToString() ?? "", InputTypes.Hidden));
                        break;
                    case PropertyTypes.Text:
                        htmlStringBuilder.AppendLine(TextEdit(thisPropertyNameParts, propertyInfo, model, resourceManager));
                        break;
                    case PropertyTypes.Number:
                        htmlStringBuilder.AppendLine(NumericEdit(thisPropertyNameParts, propertyInfo, model, resourceManager));
                        break;
                    case PropertyTypes.Collection:
                        htmlStringBuilder.AppendLine(CollectionEdit(htmlHelper, propertyInfo, model, propertyNameParts));
                        break;
                    case PropertyTypes.Complex:
                        htmlStringBuilder.AppendLine(Editor(htmlHelper, propertyInfo.GetValue(model), thisPropertyNameParts));
                        break;
                    case PropertyTypes.Unknown:
                        htmlStringBuilder.AppendLine(EditorNotImplemented(PropertyTypes.Unknown, propertyInfo));
                        break;
                }
            }

            return htmlStringBuilder.ToString();
        }

        private static string NumericEdit(List<string> propertyNameParts, PropertyInfo propertyInfo, object objectInstance, ResourceManager resourceManager)
        {
            var tagBuilder = new TagBuilder("div");
            tagBuilder.AddCssClass("form-group");

            tagBuilder.InnerHtml = Label(propertyNameParts, propertyInfo, resourceManager);
            var value = propertyInfo.GetValue(objectInstance);
            tagBuilder.InnerHtml += Input(propertyNameParts, value == null ? "" : value.ToString(), InputTypes.Numeric);

            return tagBuilder.ToString(TagRenderMode.Normal);
        }

        private static string TextEdit(List<string> propertyNameParts, PropertyInfo propertyInfo, object objectInstance, ResourceManager resourceManager)
        {
            var tagBuilder = new TagBuilder("div");
            tagBuilder.AddCssClass("form-group");

            tagBuilder.InnerHtml = Label(propertyNameParts, propertyInfo, resourceManager);
            var value = propertyInfo.GetValue(objectInstance);
            tagBuilder.InnerHtml += Input(propertyNameParts, value == null ? "" : value.ToString(), InputTypes.Text);

            return tagBuilder.ToString(TagRenderMode.Normal);
        }

        private static string CollectionEdit(HtmlHelper htmlHelper, PropertyInfo propertyInfo, object objectInstance, List<string> propertyNameParts)
        {
            StringBuilder htmlStringBuilder = new StringBuilder();

            var listType = propertyInfo.PropertyType.GetGenericArguments().Single();
            var resourceManager = LoadResources(listType);

            switch (GetCollectionItemType(propertyInfo))
            {
                case PropertyTypes.Text:
                    return StringCollectionEdit(htmlHelper, propertyInfo, objectInstance, propertyNameParts, resourceManager);
                case PropertyTypes.Number:
                    htmlStringBuilder.AppendLine(EditorNotImplemented(PropertyTypes.Collection, propertyInfo));
                    break;
                case PropertyTypes.Collection:
                    htmlStringBuilder.AppendLine(EditorNotImplemented(PropertyTypes.Collection, propertyInfo));
                    break;
                case PropertyTypes.Complex:
                    return ComplexCollectionEdit(htmlHelper, propertyInfo, objectInstance, propertyNameParts, resourceManager);
                default:
                    htmlStringBuilder.AppendLine(EditorNotImplemented(PropertyTypes.Collection, propertyInfo));
                    break;
            }

            return htmlStringBuilder.ToString();
        }

        private static string ComplexCollectionEdit(HtmlHelper htmlHelper, PropertyInfo propertyInfo, object objectInstance, List<string> propertyNameParts, ResourceManager resourceManager)
        {
            StringBuilder htmlStringBuilder = new StringBuilder();

            var model = new ComplexCollectionEditViewModel
            {
                LabelText = GetLabelText(propertyInfo, resourceManager),
                PropertyName = string.Join(".", propertyNameParts)
            };

            var index = 0;
            foreach (var item in (IEnumerable)propertyInfo.GetValue(objectInstance))
            {
                model.Items.Add(new ComplexCollectionEditItemViewModel
                {
                    PropertyName = model.PropertyName,
                    ItemIndex = index++,
                    Value = item == null ? "" : item.ToString()
                });
            }

            htmlStringBuilder.AppendLine(GetView(htmlHelper, "~/Views/ExtensionConfigurationEdits/ComplexCollectionEdit.cshtml", model));

            return htmlStringBuilder.ToString();
        }

        private static string StringCollectionEdit(HtmlHelper htmlHelper, PropertyInfo propertyInfo, object objectInstance, List<string> propertyNameParts, ResourceManager resourceManager)
        {
            StringBuilder htmlStringBuilder = new StringBuilder();

            var model = new StringCollectionEditViewModel
            {
                LabelText = GetLabelText(propertyInfo, resourceManager),
                PropertyName = string.Join(".", propertyNameParts)
            };

            var index = 0;
            foreach (var item in (IEnumerable)propertyInfo.GetValue(objectInstance))
            {
                model.Items.Add(new StringCollectionEditItemViewModel
                {
                    PropertyName = model.PropertyName,
                    ItemIndex = index++,
                    Value = item == null ? "" : item.ToString()
                });
            }

            htmlStringBuilder.AppendLine(GetView(htmlHelper, "~/Views/ExtensionConfigurationEdits/StringCollectionEdit.cshtml", model));

            return htmlStringBuilder.ToString();
        }

        private static string EditorNotImplemented(PropertyTypes propertyType, PropertyInfo propertyInfo)
        {
            var tagBuilder = new TagBuilder("h3");

            tagBuilder.InnerHtml = $"There is no editor implemented for the {propertyType} property type - {propertyInfo.PropertyType}";

            return tagBuilder.ToString(TagRenderMode.Normal);
        }
        #endregion

        #region Public Methods
        public static MvcHtmlString JobEditor(this HtmlHelper htmlHelper, EditJobViewModel model)
        {
            return MvcHtmlString.Create(Editor(htmlHelper, model, new List<string>()));
        }
        #endregion
    }
}