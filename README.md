# Convert Html to PDF Azure Function

Have you also been surprised Microsoft doesn't provide an easy way in Power Platform to generate PDF documents. Sure you have the OneDrive for Business route, but if you want to build an integration pipeline without having to license an integration account for OneDrive you are stuck with 3rd party solutions like those from Adobe or Plumsail. When you are building solutions at scale, these are great! You get professional support and a battletested solution.

If you are however already using Azure functions and are not afraid to host a PDF convert solution yourself, you can check out this function. We use it internally and if you are not spitting out thousands of PDFs an hour (which might work; just not tested), it is a easy solution to work with.

It is based of a blog post by ADITYA DESHPANDE
https://adityadeshpandeadi.wordpress.com/2020/02/02/how-to-run-a-windows-executable-inside-azure-functions-v2/ and simply runs the **wkhtmltopdf** open source tool for converting HTML to PDF.

## Usage

1. Download the binary of choice from https://wkhtmltopdf.org/ to the root of the function project (next to host.json). You can use linux, windows, 32-bit, 64-bit depending on your Function App hosting environment.
2. Ensure the project definition is aware of the executable (check out the csproj file in this project for reference).
3. Deploy your Function app. **Ensure you are using at least a Basic pricing tier for your App Hosting Plan** There are limitations to what you can do on Consumption plan sandboxes. Your processes will hang for no apparent reason. This has to do with resource consumption and limited access to lower level API's.
4. Use HTTP POST in your integration solution (for example the HTTP action in your Logic App) and paste the raw HTML as part of the body to your Function.
5. Convert the Base64 encoded binary from the body of the response to an PDF.

Example for converting back in PowerShell:

```powershell
$resp = Invoke-RestMethod -Method Post -Body $html -Uri http://localhost:7071/api/convert-Html2Pdf
[IO.File]::WriteAllBytes("c:\documents\converted.pdf", [Convert]::FromBase64String($resp))
```

Good luck! Feel free to comment with suggestions for improvement or any questions you might have!
