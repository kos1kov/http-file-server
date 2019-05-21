using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace http_file_server
{
    public class Http_server
    {
        public Http_server()
        {

        }

        public void Start()
        {
            var _listener = new HttpListener();
            try
            {
                _listener.Prefixes.Add("http://*:80/");
                _listener.Start();
                Console.WriteLine("Listening...");
                while (true)
                {
                    HttpListenerContext context = _listener.GetContext();
                    var request = context.Request.HttpMethod;
                    Console.WriteLine(request);
                    HttpListenerResponse response = context.Response;
                    try
                    {


                        switch (request)

                        {
                            case "GET":
                                getCommand(context.Request, response);
                                break;
                            case "PUT":
                                {
                                    putCommand(context.Request, response);
                                    break;
                                }
                            case "HEAD":
                                {
                                    headCommand(context.Request, response);
                                    break;
                                }
                            case "DELETE":
                                {
                                    deleteCommand(context.Request, response);
                                    break;
                                }

                        }
                    }
                    catch
                    {
                        response.StatusCode = 404;
                    }


                }
            }
            finally
            {

                _listener.Stop();
            }
        }


        public void getCommand(HttpListenerRequest request, HttpListenerResponse response)
        {
            Stream output = response.OutputStream;

            // using
            var writer = new StreamWriter(output);

            string fullPath = Directory.GetCurrentDirectory() + request.RawUrl;

            try
            {

                if (!File.Exists(fullPath))
                {
                    var result = new List<object>();
                    foreach (var entry in Directory.GetDirectories(fullPath).Concat(Directory.GetFiles(fullPath)))
                    {

                        result.Add(new
                        {
                            name = entry.Substring(Directory.GetCurrentDirectory().Length),
                            creationTime = Directory.GetCreationTime(entry)
                        });
                    }

                    writer.Write(JsonConvert.SerializeObject(result));
                    writer.Flush();
                }
                else
                {
                    try
                    {
                        using (var file = File.Open(fullPath, FileMode.Open))
                        {
                            file.CopyTo(output);
                            file.Close();
                        }


                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = 500;
                        writer.Write($"Local error happened: {ex.Message}.");
                    }
                }
            }
            finally
            {
                output.Close();
                writer.Dispose();
            }
        }

        public void putCommand(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {

                var head = request.Headers["x-copy-from"];
                string fullPath = Directory.GetCurrentDirectory() + request.RawUrl;

                if (head == null)
                {

                    var catalog = Path.GetDirectoryName(fullPath);

                    if (!Directory.Exists(catalog))
                    {
                        Directory.CreateDirectory(catalog);
                    }

                    using (var newFile = new FileStream(fullPath, FileMode.Create))
                    {
                        request.InputStream.CopyTo(newFile);

                    }

                    return;
                }
                else
                {
                    string[] list = head.Split('/');
                    try
                    {
                        File.Copy(Directory.GetCurrentDirectory() + head, fullPath + '/' + list.Last());
                    }
                    catch
                    {
                        response.StatusCode = 501;
                    }

                }

            }
            finally
            {
                response.OutputStream.Close();
            }


        }

        public void headCommand(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {

                string fullPath = Directory.GetCurrentDirectory() + request.RawUrl;
                if (Directory.Exists(fullPath))
                {
                    var info = new DirectoryInfo(fullPath);
                    response.Headers.Add("Date", info.CreationTime.ToString());
                    response.Headers.Add("Name", info.Name.ToString());
                    response.Headers.Add("directory", info.Root.ToString());
                    response.Headers.Add("attribute", info.Attributes.ToString());


                }
                else if (File.Exists(fullPath))
                {
                    FileInfo info = new FileInfo(fullPath);
                    response.Headers.Add("Date", info.CreationTime.ToString());
                    response.Headers.Add("Name", info.Name.ToString());
                    response.Headers.Add("readonly", info.IsReadOnly.ToString());
                    response.Headers.Add("length", info.Length.ToString());

                }

            }
            finally
            {
                response.OutputStream.Close();
            }


        }

        public void deleteCommand(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                string name = Directory.GetCurrentDirectory() + "/";
                string fullPath = Directory.GetCurrentDirectory() + request.RawUrl;
                if (fullPath == name)
                {
                    response.StatusCode = 403;
                    return;
                }
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
                else if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);

                }
                else
                {
                    response.StatusCode = 404;
                }

            }
            finally
            {
                response.OutputStream.Close();
            }

        }
    }
}
