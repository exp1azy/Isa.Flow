using Isa.Flow.Interact.Extractor.Rpc;
using Isa.Flow.Manager.Models;
using Isa.Flow.Manager.Models.Telegram.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Isa.Flow.Manager.Controllers
{
    /// <summary>
    /// Контроллер.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IMemoryCache _memoryCache;
        private readonly ManagerActor _managerActor;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="managerActor"></param>
        public HomeController(IConfiguration config, IMemoryCache memoryCache, ManagerActor managerActor)
        {
            _config = config;
            _memoryCache = memoryCache;
            _managerActor = managerActor;
        }

        private StateViewModel? State
        {
            get
            {
                return _memoryCache.Get<StateViewModel>("state");
            }
            set
            {
                _memoryCache.Set("state", value);
            }
        }

        /// <summary>
        /// Метод получения состояний очередей и акторов.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetState()
        {
            return new JsonResult(State);
        }

        /// <summary>
        /// Метод отображения главной страницы.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(State);
        }

        /// <summary>
        /// Метод запуска SQLExtractor.
        /// </summary>
        /// <param name="startParameters">Параметры запуска.</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> StartExtraction(StartViewModel startParameters)
        {
            if (startParameters.From != null && startParameters != null)
            {
                if (startParameters.From > startParameters.To)
                {
                    return Ok("fromMoreThanTo");
                }
            }

            await _managerActor.CallExtractorAsync(startParameters);

            return Ok($"{startParameters.Func}");
        }

        /// <summary>
        /// Метод запуска EsIndexer.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> StartIndex()
        {
            await _managerActor.CallIndexerAsync();

            return Ok("indexing");
        }

        /// <summary>
        /// Метод запуска TgCollector.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> StartTgCollection()
        {
            _memoryCache.TryGetValue("Number", out string? phoneNumber);           
            if (phoneNumber == null)
            {
                return Ok("phoneNumberRequested");
            }

            var result = await _managerActor.CallTgCollectorAsync(phoneNumber);

            return Ok(result);
        }

        /// <summary>
        /// Метод запуска VkCollector.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> StartVkCollection()
        {
            var result = await _managerActor.CallVkCollectorAsync();
            State.VkCollectorStarted = true;

            if (result != null)
            {                
                ViewBag.Html = result.Replace("\r\n", "").Replace("\n", "");
                return View("VkAuth");
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Метод запуска VkCollector с кодом.
        /// </summary>
        /// <param name="code">Код.</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> StartVkWithCode(string code)
        {
            await _managerActor.AccessTokenCallAsync(code);

            return View("Index");
        }

        /// <summary>
        /// Метод запуска TelegramCollector с другого номера.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> StartDownloadFromAnotherNumber()
        {
            _memoryCache.Remove("Number");

            return await StartTgCollection();
        }

        /// <summary>
        /// Метод, отображающий форму ввода номера телефона.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult ShowPhoneNumberForm()
        {
            return View("PhoneNumberForm", new VerificationViewModel());
        }

        /// <summary>
        /// Метод запуска TelegramCollector после установки нового номера телефона.
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> PhoneNumberForm(string phoneNumber = "")
        {
            var regex = new Regex(@"^\+\d{11}$");

            if (regex.Match(phoneNumber).Success)
            {
                _memoryCache.Set("Number", phoneNumber, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(24)));

                var result = await _managerActor.CallTgCollectorAsync(phoneNumber);

                if (result == "started")
                {
                    return RedirectToAction("Index");
                }
                else if (result == "verificationRequested")
                {
                    return RedirectToAction("ShowVerificationForm");
                }
                else
                {
                    return View(new VerificationViewModel { ErrorMessage = result });
                }
            }
            else
            {
                return View(new VerificationViewModel { ErrorMessage = "Ожидается номер в формате +79XX" });
            }
        }

        /// <summary>
        /// Метод, отображающий форму ввода кода верификации.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ShowVerificationForm()
        {
            _memoryCache.TryGetValue("Number", out string? phoneNumber);

            return View("Verification", new VerificationViewModel { Number = phoneNumber });
        }

        /// <summary>
        /// Метод установки верификационного кода.
        /// </summary>
        /// <param name="verificationCode"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Verification(string verificationCode = "")
        {
            var regex = new Regex("^\\d{5}$");

            if (regex.Match(verificationCode).Success)
            {
                await _managerActor.SetVerificationCodeCallAsync(verificationCode);

                return RedirectToAction("HomePage");
            }
            else
            {
                _memoryCache.TryGetValue("Number", out string? phoneNumber);

                return View(new VerificationViewModel
                {
                    Number = phoneNumber,
                    ErrorMessage = "Ожидается пятизначный цифровой код подтверждения"
                });
            }
        }

        /// <summary>
        /// Метод остановки TgCollector.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> StopCollection()
        {
            var result = await _managerActor.StopTgCollectorCallAsync();

            return Ok(result);
        }

        /// <summary>
        /// Метод остановки VkCollector.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> StopVkCollection()
        {
            await _managerActor.StopVkCollectorCallAsync();

            State.VkCollectorStarted = false;

            return View("Index");
        }

        /// <summary>
        /// Метод остановки EsIndexer.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> StopIndex()
        {
            await _managerActor.StopIndexingCallAsync();

            return Ok();
        }

        /// <summary>
        /// Метод остановки SQLExtractor.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> StopExtraction(int value)
        {
            switch (value)
            {
                case 0:
                    await _managerActor.StopExtractorCallAsync(SqlExtractionFunc.New);
                    break;
                case 1:
                    await _managerActor.StopExtractorCallAsync(SqlExtractionFunc.Updated);
                    break;
                case 2:
                    await _managerActor.StopExtractorCallAsync(SqlExtractionFunc.Deleted);
                    break;
            }

            return Ok();
        }

        /// <summary>
        /// Метод объявления указанной очереди.
        /// </summary>
        /// <param name="queueDeclareModel">Модель, представляющая имя очереди и лимит сообщений.</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult QueueDeclare(QueueDeclareViewModel queueDeclareModel)
        {
            var result = _managerActor.DeclareQueue(queueDeclareModel.QueueName, queueDeclareModel.Limit);

            return Ok(result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}