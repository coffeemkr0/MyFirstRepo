﻿using Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions
{
    [Export(typeof(Step))]
    [ExportMetadata("Name","CustomStep2")]
    public class CustomStep2 : Step
    {
        [ImportingConstructor]
        public CustomStep2([Import("StepConfiguration")]StepConfiguration customStep2Configuration) : base(customStep2Configuration)
        {

        }

        public override void Execute()
        {
            Console.WriteLine($"Execute custom step 2 - property value={((CustomStep2Configuration)this.StepConfiguration).CustomStep2ConfigurationProperty}");
        }
    }
}
