using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
            ILogger log, ExecutionContext context)
        {
            string TempFolder = Environment.GetEnvironmentVariable("TMP");
            string ExeLocation = $"{context.FunctionAppDirectory}\\wkhtmltopdf.exe";
            string base64FileString = "";
            string output = "";
            try
            {
                //Ensure unique names
                string guid = Guid.NewGuid().ToString();
                string htmlFile = $"{TempFolder}\\{guid}.htm";
                string pdfFile = $"{TempFolder}\\{guid}.pdf";

                //Write HTML file to TMP folder
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                File.WriteAllText(htmlFile, requestBody);

                //Run process to convert the HTML file to PDF
                ProcessStartInfo info = new ProcessStartInfo
                {
                    WorkingDirectory = TempFolder,
                    FileName = ExeLocation,
                    Arguments = $" {htmlFile} {pdfFile}",
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process proc = new Process
                {
                    StartInfo = info
                };
                proc.Refresh();
                proc.StartInfo.RedirectStandardOutput = true;
                proc.Start();
                //If any output write to logs
                while (!proc.StandardOutput.EndOfStream)
                {
                    output = proc.StandardOutput.ReadLine();
                }
                log.LogInformation(output);
                //Wait for process exit.
                proc.WaitForExit();
                
                //Read the PDF file and convert to Base64 string
                var fileBytes = File.ReadAllBytes(pdfFile);
                base64FileString = Convert.ToBase64String(fileBytes);


            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return (ActionResult)new StatusCodeResult(500);
            }
            
            //Include base64 string in result
            return (ActionResult)new OkObjectResult(base64FileString);

        }
    }
}
