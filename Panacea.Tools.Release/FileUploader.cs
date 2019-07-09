using PluginPackager2.Models;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Tools.Release
{
    public static class FileUploader
    {
        static void OnUploadProgress(int progress)
        {
            if (UploadProgressChanged != null)
                UploadProgressChanged(null, progress);
        }
        public static event EventHandler<int> UploadProgressChanged;
        public static async Task<GenericResponse> UploadFile(String path, String actionUrl)
        {
            return await Task<GenericResponse>.Run(() =>
            {
                HttpWebRequest requestToServerEndpoint = (HttpWebRequest)WebRequest.Create(actionUrl);
                OnUploadProgress(0);
                string boundaryString = Guid.NewGuid().ToString();//"----SomeRandomText";
                string fileUrl = path;

                // Set the http request header \\
                requestToServerEndpoint.Method = WebRequestMethods.Http.Post;
                requestToServerEndpoint.ContentType = "multipart/form-data; boundary=" + boundaryString;
                requestToServerEndpoint.KeepAlive = true;
                requestToServerEndpoint.Credentials = System.Net.CredentialCache.DefaultCredentials;

                // Use a MemoryStream to form the post data request,
                // so that we can get the content-length attribute.
                MemoryStream postDataStream = new MemoryStream();
                StreamWriter postDataWriter = new StreamWriter(postDataStream);

                // Include value from the myFileDescription text area in the post data
                postDataWriter.Write("\r\n--" + boundaryString + "\r\n");
                postDataWriter.Write("Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}",
                                        "robocop",
                                        "A sample file description");

                // Include the file in the post data
                postDataWriter.Write("\r\n--" + boundaryString + "\r\n");
                postDataWriter.Write("Content-Disposition: form-data;"
                                        + "name=\"{0}\";"
                                        + "filename=\"{1}\""
                                        + "\r\nContent-Type: {2}\r\n\r\n",
                                        "myfile",
                                        Path.GetFileName(fileUrl),
                                        Path.GetExtension(fileUrl));
                postDataWriter.Flush();

                // Read the file
                FileStream fileStream = new FileStream(fileUrl, FileMode.Open, FileAccess.Read);

                

                byte[] buffer = new byte[1024];
                
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    postDataStream.Write(buffer, 0, bytesRead);
                   
                }
                fileStream.Close();

                postDataWriter.Write("\r\n--" + boundaryString + "--\r\n");
                postDataWriter.Flush();

                // Set the http request body content length
                requestToServerEndpoint.ContentLength = postDataStream.Length;
                requestToServerEndpoint.Timeout = 500000000;

                // Dump the post data from the memory stream to the request stream
                using (Stream s = requestToServerEndpoint.GetRequestStream())
                {
                    int chunkSize = 1024;
                    int chunks = Convert.ToInt32(postDataStream.Length / chunkSize);
                    int lastOne = Convert.ToInt32(postDataStream.Length % chunkSize);


                    int written = 0;
                    postDataStream.Seek(0, SeekOrigin.Begin);
                    long maxLength = postDataStream.Length;
                    while (written < postDataStream.Length)
                    {
                        written += chunkSize;
                        byte[] chunkData = new byte[chunkSize];
                        if (written > postDataStream.Length)
                        {
                            int lastLength = Convert.ToInt32(postDataStream.Length - (written - chunkSize));
                            chunkData = new byte[lastLength];
                        }
                        postDataStream.Read(chunkData, 0, chunkData.Length);
                        try
                        {
                            s.Write(chunkData, 0, chunkData.Length);
                        }
                        catch {
                            Utils.Panic("Upload failed. QQ");
                            return null;
                        }
                        postDataStream.Flush();
                        s.Flush();

                        OnUploadProgress((int)((double)written / (double)maxLength * 100.0));
                    }
                    
                }
                postDataStream.Flush();
                postDataStream.Close();
                try
                {
                    using (StreamReader sr = new StreamReader(requestToServerEndpoint.GetResponse().GetResponseStream()))
                    {
                        String result = sr.ReadToEnd();
                        var json = JsonSerializer.DeserializeFromString<GenericResponse>(result);
                        return json;
                    }
                }
                catch (Exception ex)
                {
                    return new GenericResponse() { Success = false, Message = ex.Message };
                }
            });


        }
    }

}
