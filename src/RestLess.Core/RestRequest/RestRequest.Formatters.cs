﻿using System.IO;
using System.Net.Http;
using RestLess.Http;

namespace RestLess.Internal
{
    internal sealed partial class RestRequest
    {
        public IRestRequest WithMediaTypeFormatter(string mediaTypeFormatterName)
        {
            this.mediaTypeFormatter = this.restClient.Settings.MediaTypeFormatters.Get(mediaTypeFormatterName);
            return this;
        }

        public IRestRequest WithUrlParameterFormatter(string urlParameterFormatterName)
        {
            this.valueFormatter = this.restClient.Settings.UrlParameterFormatters.Get(urlParameterFormatterName);
            return this;
        }

        public IRestRequest WithFormFormatter(string formFormatterName)
        {
            this.formFormatter = this.restClient.Settings.FormFormatters.Get(formFormatterName);
            return this;
        }
    }
}
