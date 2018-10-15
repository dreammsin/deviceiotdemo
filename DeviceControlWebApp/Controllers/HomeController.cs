using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Configuration;

namespace DeviceControlWebApp.Controllers
{
    public class HomeController : Controller
    {
        private static string DeviceID = ConfigurationManager.AppSettings["DeviceID"] ?? "";
        private static string DeviceName = ConfigurationManager.AppSettings["DeviceName"] ?? "";
        private static string DeviceToken = ConfigurationManager.AppSettings["DeviceToken"] ?? "";
        private const string ParticleApi = "https://api.particle.io/v1/devices/";
        private string deviceCall = "{devicename}/{function}/?access_token={accesstoken}";
        private static RestClient restClient;
        private static CancellationTokenSource tokenSource = new CancellationTokenSource();
        public static bool inAutomation { get; set; }

        static HomeController()
        {
            inAutomation = false;
            // Rest client that is used to exercise Particle.io api
            restClient = new RestClient(ParticleApi);
            restClient.AddDefaultUrlSegment("devicename", DeviceName);
            restClient.AddDefaultUrlSegment("accesstoken", DeviceToken);
        }

        public async Task<ActionResult> Index()

        {
            ViewBag.inAuto = inAutomation;
            // Get all status from Particle.io
            var photoLevelR = await restClient.ExecuteGetTaskAsync<VariableResponse<int>>(new RestRequest(deviceCall).AddUrlSegment("function", "photoLevel"));
            var flashTimeR = restClient.ExecuteGetTaskAsync<VariableResponse<int>>(new RestRequest(deviceCall).AddUrlSegment("function", "flashTime"));
            var lampStatusR = restClient.ExecuteGetTaskAsync<VariableResponse<bool>>(new RestRequest(deviceCall).AddUrlSegment("function", "lampStatus"));
            var sendToCloudR = restClient.ExecuteGetTaskAsync<VariableResponse<bool>>(new RestRequest(deviceCall).AddUrlSegment("function", "sendToCloud"));
            var blueStatusR = restClient.ExecuteGetTaskAsync<VariableResponse<bool>>(new RestRequest(deviceCall).AddUrlSegment("function", "blueStatus"));
            var redStatusR = restClient.ExecuteGetTaskAsync<VariableResponse<bool>>(new RestRequest(deviceCall).AddUrlSegment("function", "redStatus"));
            var cloudStatusR = restClient.ExecuteGetTaskAsync<VariableResponse<bool>>(new RestRequest(deviceCall).AddUrlSegment("function", "cloudStatus"));
            var manualStatusR = restClient.ExecuteGetTaskAsync<VariableResponse<bool>>(new RestRequest(deviceCall).AddUrlSegment("function", "manualStatus"));
            // Execute all tasks and wait here
            await Task.WhenAll(flashTimeR, lampStatusR, sendToCloudR, blueStatusR, redStatusR, cloudStatusR, manualStatusR);

            // Assign to View variables for display
            ViewBag.deviceID = DeviceID;
            ViewBag.deviceName = DeviceName;
            ViewBag.photoLevel = photoLevelR.Data.result;
            ViewBag.lampStatus = lampStatusR.Result.Data.result;
            ViewBag.flashTime = flashTimeR.Result.Data.result;
            ViewBag.sendToCloud = sendToCloudR.Result.Data.result;
            ViewBag.blueStatus = blueStatusR.Result.Data.result;
            ViewBag.redStatus = redStatusR.Result.Data.result;
            ViewBag.cloudStatus = cloudStatusR.Result.Data.result;
            ViewBag.manualStatus = manualStatusR.Result.Data.result;
            ViewBag.Message = "Flash time: " + ViewBag.flashTime;

            // Render view
            return View();
        }

        public async Task<ActionResult> SendToCloudOn()
        {
            await restClient.ExecutePostTaskAsync(new RestRequest(deviceCall).AddUrlSegment("function", "toCloudON"));
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> SendToCloudOff()
        {
            await restClient.ExecutePostTaskAsync(new RestRequest(deviceCall).AddUrlSegment("function", "toCloudOFF"));
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> ToggleLamp()
        {
            await restClient.ExecutePostTaskAsync(new RestRequest(deviceCall).AddUrlSegment("function", "toggleLamp"));
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> FlashBlueLED(string id, string flashInput)
        {
            var timeInput = flashInput; //"200";
            var timeSet = 0;
            var timeSetStr = "";
            if (int.TryParse(timeInput, out timeSet))
            {
                timeSetStr = timeSet.ToString();
                await restClient.ExecutePostTaskAsync(new RestRequest(deviceCall).AddUrlSegment("function", "flashBlue").AddHeader("content-type", "application/json").AddJsonBody(new { args = timeSetStr }));
            }
            else
            {
                await restClient.ExecutePostTaskAsync(new RestRequest(deviceCall).AddUrlSegment("function", "flashBlue"));
                ViewBag.Err = "Value is invalid: '" + timeInput;
            }
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> StartAutomate()
        {
            inAutomation = true;
            ViewBag.inAuto = inAutomation;
            await restClient.ExecutePostTaskAsync(new RestRequest(deviceCall).AddUrlSegment("function", "toCloudON"));
            var t = Task.Run(async delegate
            {
                await AutomationLoop(tokenSource.Token);
            });
            return RedirectToAction("Index");
        }
        public async Task<ActionResult> StopAutomate()
        {
            inAutomation = false;
            ViewBag.inAuto = inAutomation;
            await restClient.ExecutePostTaskAsync(new RestRequest(deviceCall).AddUrlSegment("function", "toCloudOFF"));
            tokenSource.Cancel();
            tokenSource.Dispose();
            return RedirectToAction("Index");
        }

        private async Task AutomationLoop(CancellationToken token)
        {
            Random newRand = new Random();
            int newSpan = Math.Min(30,newRand.Next(900)) ;    //wait time
            int flashD = newSpan * 1000;     //flash time
            var nextT = DateTime.Now.AddSeconds(newSpan);
            var currentT = DateTime.Now;
            while (inAutomation)
            {
                if (currentT <= nextT)
                {
                    //do nothing
                    await Task.Delay(100);

                }
                else
                {
                    await restClient.ExecutePostTaskAsync(new RestRequest(deviceCall).AddUrlSegment("function", "toggleLamp"));
                    await restClient.ExecutePostTaskAsync(new RestRequest(deviceCall).AddUrlSegment("function", "flashBlue").AddHeader("content-type", "application/json").AddJsonBody(new { args = flashD.ToString() }));
                    newSpan = newRand.Next(300);    //wait time
                    flashD = newSpan * 1000;     //flash time
                    nextT = DateTime.Now.AddSeconds(newSpan);
                }
                currentT = DateTime.Now;
            }
        }
    }

}