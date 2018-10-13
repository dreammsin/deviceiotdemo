using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace DeviceControlWebApp.Controllers
{
    public class Device
    {
        public int flashTimer { get; set; }
        public string flashMsg { get; set; }
    }

    public class HomeController : Controller
    {
        private const string DeviceID = "1a002b001247343432313031";
        private const string DeviceName = "monkey_hunter";
        private const string useToken = "79141e26e2e8005c1dbb03d5eba780f479bf9cb3";
        private const string ParticleApi = "https://api.particle.io/v1/devices/";
        private string deviceCall = "{devicename}/{function}/?access_token={accesstoken}";
        private static RestClient restClient;

        //public static Device device = new Device();

        static HomeController()
        {
            // Rest client that is used to exercise Particle.io api
            restClient = new RestClient(ParticleApi);
            restClient.AddDefaultUrlSegment("devicename", DeviceName);
            restClient.AddDefaultUrlSegment("accesstoken", useToken);
        }

        public async Task<ActionResult> Index()

        {
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
            //ViewData[photoLevelR.Result.Data.name] = photoLevelR.Result.Data.result;
            //ViewData[lampStatusR.Result.Data.name] = lampStatusR.Result.Data.result;
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
            //ViewBag.flashDuration = ViewBag.flashTime;
            //ViewBag.flashMsg = "Flash time: " + ViewBag.flashTime;
            //ViewData["flash"] = ViewBag.flashTime;
            ViewBag.Message = "Initial Flash time: " + ViewBag.flashTime;
            //device.flashTimer = ViewBag.flashTime;
            //device.flashMsg = "Flash time: " + ViewBag.flashTime;

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
                //ViewData["msg"] = "Value is invalid: '" + timeInput + "'. Using duration: " ;
                await restClient.ExecutePostTaskAsync(new RestRequest(deviceCall).AddUrlSegment("function", "flashBlue"));
                ViewBag.Err = "Value is invalid: '" + timeInput;
            }
            return RedirectToAction("Index");
        }
    }

}