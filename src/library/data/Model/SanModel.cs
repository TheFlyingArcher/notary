﻿using System;
using System.Collections.Generic;
using System.Text;
using Notary.Contract;

namespace Notary.Data.Model
{
    public class SanModel
    {
        public SanModel()
        {
        }

        /// <summary>
        /// The kind of SAN
        /// </summary>
        public SanKind Kind
        {
            get; set;
        }

        /// <summary>
        /// Subject Alternative Name. Can be a DNS or e-mail
        /// </summary>
        public string Name
        {
            get; set;
        }
    }
}
