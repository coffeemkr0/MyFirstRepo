﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cloudflow.Web.ViewModels.ExtensionConfigurationEdits
{
    public class ComplexCollectionEditViewModel
    {
        public string LabelText { get; set; }

        public string PropertyName { get; set; }

        public List<ComplexCollectionEditItemViewModel> Items { get; set; }

        public ComplexCollectionEditViewModel()
        {
            this.Items = new List<ComplexCollectionEditItemViewModel>();
        }
    }
}