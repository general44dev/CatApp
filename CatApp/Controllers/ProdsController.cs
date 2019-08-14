using CatApp.Models;
using CatApp.Services;
using CatApp.Utilities;
using CatApp.ViewModels;
using CatApp.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CatApp.Controllers
{
    public class ProdsController : Controller

    {
        private readonly ProductService _productService;
        private readonly long _fileSizeLimit;
        private readonly string[] _permittedExtensions = { ".txt", ".pdf", ".jpg", ".jpeg", };


        // Get the default form options so that we can use them to set the default 
        // limits for request body data.
        private static readonly FormOptions _defaultFormOptions = new FormOptions();



        public ProdsController(ProductService productService, IConfiguration config)
        {
            _productService = productService;
            _fileSizeLimit = config.GetValue<long>("FileSizeLimit");

        }


        // GET: Prods
        public ActionResult Index()

        {
            var prods = _productService.Get().AsQueryable().
               Select(p => new ProdVM(p.Id, p.ProductName, p.Stock, p.Category, p.Description, p.ProdPhoto.Id, p.ProdPhoto.Content, p.ProdPhoto.UntrustedName, p.ProdPhoto.Note, p.ProdPhoto.Size, p.ProdPhoto.UploadDT));

            return View(prods.ToList());

        }

        // GET: Prods/Details/5
        public ActionResult Details(string id)

        {
            var p = _productService.Get(id);

            if (p == null)
            {
                return NotFound();
            }
            var prod = new ProdVM(p.Id, p.ProductName, p.Stock, p.Category, p.Description, p.ProdPhoto.Id, p.ProdPhoto.Content, p.ProdPhoto.UntrustedName, p.ProdPhoto.Note, p.ProdPhoto.Size, p.ProdPhoto.UploadDT);
            return View(model: prod);
        }

        // GET: Prods/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Prods/Create
        [HttpPost]
        [DisableFormValueModelBinding]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync()
        {
            try
            {
                // TODO: Add insert logic here

                if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
                {
                    ModelState.AddModelError("File",
                        $"The request couldn't be processed (Error 1).");
                    // Log error

                    return BadRequest(ModelState);
                }

                // Accumulate the form data key-value pairs in the request (formAccumulator).
                var formAccumulator = new KeyValueAccumulator();
                var trustedFileNameForDisplay = string.Empty;
                var untrustedFileNameForStorage = string.Empty;
                var streamedFileContent = new byte[0];

                var boundary = MultipartRequestHelper.GetBoundary(
                    MediaTypeHeaderValue.Parse(Request.ContentType),
                    _defaultFormOptions.MultipartBoundaryLengthLimit);
                var reader = new MultipartReader(boundary, HttpContext.Request.Body);

                var section = await reader.ReadNextSectionAsync();

                while (section != null)
                {
                    var hasContentDispositionHeader =
                        ContentDispositionHeaderValue.TryParse(
                            section.ContentDisposition, out var contentDisposition);

                    if (hasContentDispositionHeader)
                    {
                        if (MultipartRequestHelper
                            .HasFileContentDisposition(contentDisposition))
                        {
                            untrustedFileNameForStorage = contentDisposition.FileName.Value;
                            // Don't trust the file name sent by the client. To display
                            // the file name, HTML-encode the value.
                            trustedFileNameForDisplay = WebUtility.HtmlEncode(
                                    contentDisposition.FileName.Value);

                            streamedFileContent =
                                await FileHelpers.ProcessStreamedFile(section, contentDisposition,
                                    ModelState, _permittedExtensions, _fileSizeLimit);

                            if (!ModelState.IsValid)
                            {
                                return BadRequest(ModelState);
                            }
                        }
                        else if (MultipartRequestHelper
                            .HasFormDataContentDisposition(contentDisposition))
                        {
                            // Don't limit the key name length because the 
                            // multipart headers length limit is already in effect.
                            var key = HeaderUtilities
                                .RemoveQuotes(contentDisposition.Name).Value;
                            var encoding = GetEncoding(section);

                            if (encoding == null)
                            {
                                ModelState.AddModelError("File",
                                    $"The request couldn't be processed (Error 2).");
                                // Log error

                                return BadRequest(ModelState);
                            }

                            using (var streamReader = new StreamReader(
                                section.Body,
                                encoding,
                                detectEncodingFromByteOrderMarks: true,
                                bufferSize: 1024,
                                leaveOpen: true))
                            {
                                // The value length limit is enforced by 
                                // MultipartBodyLengthLimit
                                var value = await streamReader.ReadToEndAsync();

                                if (string.Equals(value, "undefined",
                                    StringComparison.OrdinalIgnoreCase))
                                {
                                    value = string.Empty;
                                }

                                formAccumulator.Append(key, value);

                                if (formAccumulator.ValueCount >
                                    _defaultFormOptions.ValueCountLimit)
                                {
                                    // Form key count limit of 
                                    // _defaultFormOptions.ValueCountLimit 
                                    // is exceeded.
                                    ModelState.AddModelError("File",
                                        $"The request couldn't be processed (Error 3).");
                                    // Log error

                                    return BadRequest(ModelState);
                                }
                            }
                        }
                    }

                    // Drain any remaining section body that hasn't been consumed and
                    // read the headers for the next section.
                    section = await reader.ReadNextSectionAsync();
                }

                // Bind form data to the model
                var formData = new FormData();
                var formValueProvider = new FormValueProvider(
                    BindingSource.Form,
                    new FormCollection(formAccumulator.GetResults()),
                    CultureInfo.CurrentCulture);
                var bindingSuccessful = await TryUpdateModelAsync(formData, prefix: "",
                    valueProvider: formValueProvider);

                if (!bindingSuccessful)
                {
                    ModelState.AddModelError("File",
                        "The request couldn't be processed (Error 5).");
                    // Log error

                    return BadRequest(ModelState);
                }

                // **WARNING!**
                // In the following example, the file is saved without
                // scanning the file's contents. In most production
                // scenarios, an anti-virus/anti-malware scanner API
                // is used on the file before making the file available
                // for download or for use by other systems. 
                // For more information, see the topic that accompanies 
                // this sample app.

                var file = new AppFile()
                {
                    Content = streamedFileContent,
                    UntrustedName = untrustedFileNameForStorage,
                    Note = formData.Note,
                    Size = streamedFileContent.Length,
                    UploadDT = DateTime.UtcNow
                };
                var prod = new Product
                {
                    ProductName = formData.Name,
                    Stock = formData.Stock,
                    Category = formData.Categoria,
                    Description = formData.Description,
                    ProdPhoto = new ProdPhoto
                    {
                        Content = streamedFileContent,
                        UntrustedName = untrustedFileNameForStorage,
                        Note = formData.Note,
                        Size = streamedFileContent.Length,
                        UploadDT = DateTime.UtcNow
                    }
                };

                prod = _productService.Create(prod);




                return Created(nameof(ProdsController), null);



                //--------------------------------
                // return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Prods/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Prods/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Prods/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Prods/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }


        private static Encoding GetEncoding(MultipartSection section)
        {
            var hasMediaTypeHeader =
                MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);

            // UTF-7 is insecure and shouldn't be honored. UTF-8 succeeds in 
            // most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }

            return mediaType.Encoding;
        }
    }

    public class FormData
    {
        public string Name { get; set; }
        public string Note { get; set; }
        public string Description { get; set; }
        public int Stock { get; set; }

        public string Categoria { get; set; }
    }

}