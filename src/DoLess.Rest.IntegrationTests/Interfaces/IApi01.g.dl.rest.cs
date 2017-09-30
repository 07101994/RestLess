﻿using System.Net.Http;
using System.Threading.Tasks;
using DoLess.Rest.IntegrationTests.Interfaces;

/*
* This file is generated by DoLess.Rest.
* All modifications will be erased at the next build.
* Please modify the dependent interface in order to change this file.
*/
namespace DoLess.Rest.Generated
{
    internal sealed class RestClientForIApi01 : RestClientBase, IApi01
    {
        public Task<HttpResponseMessage> DeleteAsync()
        {
            return RestRequest.Delete(this)
                              .WithUriTemplate("api/posts")
                              .ReadAsHttpResponseMessageAsync();
        }

        public Task<HttpResponseMessage> GetAsync()
        {
            return RestRequest.Get(this)
                              .WithUriTemplate("api/posts")
                              .ReadAsHttpResponseMessageAsync();
        }

        public Task<HttpResponseMessage> HeadAsync()
        {
            return RestRequest.Head(this)
                              .WithUriTemplate("api/posts")
                              .ReadAsHttpResponseMessageAsync();
        }

        public Task<HttpResponseMessage> OptionsAsync()
        {
            return RestRequest.Options(this)
                              .WithUriTemplate("api/posts")
                              .ReadAsHttpResponseMessageAsync();
        }

        public Task<HttpResponseMessage> PatchAsync()
        {
            return RestRequest.Patch(this)
                              .WithUriTemplate("api/posts")
                              .ReadAsHttpResponseMessageAsync();
        }

        public Task<HttpResponseMessage> PostAsync()
        {
            return RestRequest.Post(this)
                              .WithUriTemplate("api/posts")
                              .ReadAsHttpResponseMessageAsync();
        }

        public Task<HttpResponseMessage> PutAsync()
        {
            return RestRequest.Put(this)
                              .WithUriTemplate("api/posts")
                              .ReadAsHttpResponseMessageAsync();
        }

        public Task<HttpResponseMessage> TraceAsync()
        {
            return RestRequest.Trace(this)
                              .WithUriTemplate("api/posts")
                              .ReadAsHttpResponseMessageAsync();
        }
    }
}