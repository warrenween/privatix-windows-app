using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Privatix.Core;


namespace Privatix.Service
{
    class HttpService
    {
        public void Start()
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes("OK");
            HttpListener listener = new HttpListener();
            string prefix = String.Format("http://{0}:{1}/", "127.0.0.1", Config.WebCheckPort);
            listener.Prefixes.Add(prefix);
            prefix = String.Format("http://{0}:{1}/", "localhost", Config.WebCheckPort);
            listener.Prefixes.Add(prefix);
            prefix = String.Format("https://{0}:{1}/", "127.0.0.1", Config.WebCheckPortHttps);
            listener.Prefixes.Add(prefix);
            prefix = String.Format("https://{0}:{1}/", "localhost", Config.WebCheckPortHttps);
            listener.Prefixes.Add(prefix);
            listener.Start();

            while (true)
            {
                //Ожидание входящего запроса
                HttpListenerContext context = listener.GetContext();

                //Объект запроса
                HttpListenerRequest request = context.Request;

                //Объект ответа
                HttpListenerResponse response = context.Response;

                //Создаем ответ
                string requestBody;
                Stream inputStream = request.InputStream;
                Encoding encoding = request.ContentEncoding;
                StreamReader reader = new StreamReader(inputStream, encoding);
                requestBody = reader.ReadToEnd();
                response.StatusCode = (int)HttpStatusCode.OK;
                response.AddHeader("Access-Control-Allow-Origin", "*");

                //Возвращаем ответ
                using (Stream stream = response.OutputStream) 
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
