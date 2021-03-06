﻿using System;
using System.Collections.Generic;

namespace Cloudflow.Web.ViewModels.ExtensionConfigurationEdits
{
    public class TabHeaderViewModel
    {
        #region Properties
        public List<TabHeaderItem> Items { get; set; }
        #endregion

        #region Constructors
        public TabHeaderViewModel()
        {
            Items = new List<TabHeaderItem>();
        }
        #endregion

        #region TabHeaderItem
        public class TabHeaderItem
        {
            #region Properties
            public Guid Id { get; set; }

            public bool Active { get; set; }

            public string DisplayText { get; set; }
            #endregion
        }
        #endregion
    }
}