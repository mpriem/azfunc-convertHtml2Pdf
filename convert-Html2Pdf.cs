using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

//https://adityadeshpandeadi.wordpress.com/2020/02/02/how-to-run-a-windows-executable-inside-azure-functions-v2/

namespace Qoders.Az.Functions
{
    public static class convertHtml2Pdf
    {
        [FunctionName("convert-Html2Pdf")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            log.LogInformation($"Starting {context.FunctionName}");
            string WorkDir = Environment.GetEnvironmentVariable("TMP");
            string ExeLocation = $"{context.FunctionAppDirectory}\\wkhtmltopdf.exe";
            string base64FileString = "";
            string guid = Guid.NewGuid().ToString();
            string htmlFile = $"{WorkDir}\\{guid}.htm";
            string pdfFile = $"{WorkDir}\\{guid}.pdf";

            try
            {
                if(!File.Exists(ExeLocation)){
                    throw new FileNotFoundException($"{ExeLocation} does not exist");
                }

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogDebug($"Body: `n {requestBody}");
                File.WriteAllText(htmlFile, requestBody);

                log.LogInformation($"HTML File created: {File.Exists(htmlFile)}");

                log.LogInformation("Starting pdf conversion");
                int timeout = 30000;
                using (Process process = new Process())
                {
                    ProcessStartInfo pInfo = new ProcessStartInfo
                    {
                        WorkingDirectory = WorkDir,
                        FileName = ExeLocation,
                        Arguments = $"-q {htmlFile} {pdfFile}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = false
                    };
                    process.StartInfo = pInfo;

                    StringBuilder output = new StringBuilder();
                    StringBuilder error = new StringBuilder();

                    //event handlers to handle outputs
                    using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                    using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                    {
                        process.OutputDataReceived += (sender, e) => {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            {
                                output.AppendLine(e.Data);
                            }
                        };
                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                errorWaitHandle.Set();
                            }
                            else
                            {
                                error.AppendLine(e.Data);
                            }
                        };

                        process.Start();

                        // start reading output streams
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        if (process.WaitForExit(timeout) &&
                            outputWaitHandle.WaitOne(timeout) &&
                            errorWaitHandle.WaitOne(timeout))
                        {
                            log.LogInformation($"PDF File created: {File.Exists(pdfFile)}");
                            if(!File.Exists(pdfFile)){
                                throw new FileNotFoundException($"{pdfFile} not found");
                            }
                            var fileBytes = File.ReadAllBytes(pdfFile);
                            base64FileString = Convert.ToBase64String(fileBytes);
                        }
                        else
                        {
                            throw new Exception("Operation timed out");
                        }
                    }
                    log.LogInformation($"Output: {output.ToString()}");
                    log.LogError($"Errors: {error.ToString()}");
                }
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return (ActionResult)new StatusCodeResult(500);
            }
            finally{
                if(File.Exists(htmlFile)){ File.Delete(htmlFile); } 
                if(File.Exists(pdfFile)){ File.Delete(pdfFile); } 
                log.LogInformation($"Stopping {context.FunctionName}");
            }
            return (ActionResult)new OkObjectResult(base64FileString);
        }
    }
}
