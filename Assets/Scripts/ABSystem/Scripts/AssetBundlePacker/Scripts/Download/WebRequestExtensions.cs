namespace game.Assets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Net;
    using UnityEngine;

    public static class WebRequestExtensions
    {
       public static WebResponse BatterEndGetResponse(this WebRequest request,IAsyncResult ar)
        {
            try
            {
                return request.EndGetResponse(ar);
            }
            catch(WebException e)
            {
                if (e.Response != null)
                    return e.Response;
                throw;
            }
        }
    }
}

