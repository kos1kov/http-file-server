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
                    switch (request)
                    {
                        case "GET":
                            getCommand(context.Request, ref response);
                            break;
                        case "PUT":
                            {
                                putCommand(context.Request, ref response);
                                break;
                            }
                        case "HEAD":
                            {
                                headCommand(context.Request, ref response);
                                break;
                            }
                        case "DELETE":
                            {
                                deleteCommand(context.Request, ref response);
                                break;
                            }

                    }


                }
            }
            finally
            {

                _listener.Stop();
            }
        }


        public void getCommand(HttpListenerRequest request, ref HttpListenerResponse response)
        {
            Stream output = response.OutputStream;

            var writer = new StreamWriter(output);

            string fullPath = Directory.GetCurrentDirectory() + request.RawUrl;

            try
            {

                if (!File.Exists(fullPath))
                {

                    var directories = Directory.GetDirectories(fullPath);
                    foreach (var entry in directories)
                    {

                        var obj = new { name = entry.Substring(Directory.GetCurrentDirectory().Length), creationTime = Directory.GetCreationTime(entry) };
                        writer.Write(JsonConvert.SerializeObject(obj));


                    }
                    foreach (var entry in Directory.GetFiles(fullPath))
                    {
                        var obj = new { name = entry.Substring(Directory.GetCurrentDirectory().Length), creationTime = Directory.GetCreationTime(entry) };
                        writer.Write(JsonConvert.SerializeObject(obj));

                    }
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
                        response.StatusCode = 400;
                        writer.Write($"Local error happened: {ex.Message}.");
                    }
                }
            }
            catch
            {
                response.StatusCode = 404;
            }
            finally
            {
                output.Close();
                writer.Dispose();
            }
        }

        public void putCommand(HttpListenerRequest request, ref HttpListenerResponse response)
        {
            try
            {
                var head = request.Headers["x-copy-from"];
                string fullPath = Directory.GetCurrentDirectory() + request.RawUrl;
                string[] list = head.Split('/');
                if (head == null)
                {
                    
                    var catalog = Path.GetDirectoryName(fullPath);

                    if (!Directory.Exists(catalog))
                    {
                        Directory.CreateDirectory(catalog);
                    }

                    using (var newFile = new FileStream(fullPath, FileMode.Create))//error if send exist directory
                    {
                        request.InputStream.CopyTo(newFile);

                    }

                    return;
                }
                else
                {
                    File.Copy(Directory.GetCurrentDirectory()+head, fullPath+list.Last());
                }
               
            }
            finally
            {
                response.OutputStream.Close();
            }


        }

        public void headCommand(HttpListenerRequest request, ref HttpListenerResponse response)
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
            catch
            {
                response.StatusCode = 404;
            }
            finally
            {
                response.OutputStream.Close();
            }


        }

        public void deleteCommand(HttpListenerRequest request, ref HttpListenerResponse response)
        {
            try
            {

                string fullPath = Directory.GetCurrentDirectory() + request.RawUrl;
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath);


                }
                else if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);

                }

            }
            catch
            {
                response.StatusCode = 404;
            }
            finally
            {
                response.OutputStream.Close();
            }

        }
    }
}
