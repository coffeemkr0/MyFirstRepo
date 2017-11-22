﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudflow.Core.Extensions.ExtensionAttributes
{
    public class CategorizedItemCollection
    {
        #region Properties
        public List<Category> Categories { get; set; }
        #endregion

        #region Constructors
        public CategorizedItemCollection()
        {
            this.Categories = new List<Category>();
        }
        #endregion

        #region Category
        public class Category
        {
            #region Properties
            public string Name { get; set; }

            public List<Item> Items { get; set; }
            #endregion

            #region Constructors
            public Category()
            {
                this.Items = new List<Item>();
            }
            #endregion

            #region Item
            public class Item
            {
                #region Properties
                public string Category { get; set; }

                public string Name { get; set; }

                public string Description { get; set; }

                public byte[] Icon { get; set; }

                public Dictionary<string, object> Values { get; set; }
                #endregion
            }
            #endregion
        }
        #endregion
    }
}
