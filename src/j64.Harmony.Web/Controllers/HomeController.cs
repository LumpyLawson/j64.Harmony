﻿using System;
using Microsoft.AspNetCore.Mvc;
using j64.Harmony.Web.Models;
using j64.Harmony.Web.Repository;
using j64.Harmony.Web.ViewModels.Home;
using j64.Harmony.Xmpp;

namespace j64.Harmony.Web.Controllers
{
    public class HomeController : Controller
    {
        private j64HarmonyGateway j64Config = null;
        private Hub myHub = null;
        private static int previousLevel { get; set; } = 50;

        private HomeViewModel myHomeViewModel
        {
            get
            {
                var hvm = new HomeViewModel()
                {
                    ChannelDevice = j64Config.ChannelDevice,
                    ChannelSurfDeviceName = j64Config.ChannelSurfDeviceName,
                    LastChannelDeviceName = j64Config.LastChannelDeviceName,
                    SoundDeviceName = j64Config.SoundDeviceName,
                    VcrPauseDeviceName = j64Config.VcrPauseDeviceName,
                    VolumeDevice = j64Config.VolumeDevice
                };
                j64Config.FavoriteChannels.ForEach(x => hvm.FavoriteChannels.Add(new Web.ViewModels.Home.FavoriteChannel() { Channel = x.Channel, Name = x.Name }));
                j64Config.CustomCommands.ForEach(x => hvm.CustomCommands.Add(new ViewModels.Home.CustomCommand() { Name = x.CommandName, NumKeyPresses = x.Actions.Count }));
                return hvm;
            }
        }

        public HomeController(j64HarmonyGateway hubConfig, Hub hub)
        {
            this.j64Config = hubConfig;
            this.myHub = hub;
        }

        public IActionResult Index()
        {
            // If not connected to the smart hub go to the configure page
            if (myHub.hubConfig == null || String.IsNullOrEmpty(j64Config.ChannelDevice) || String.IsNullOrEmpty(j64Config.VolumeDevice))
                return RedirectToAction("Edit", "FirstTimeConfig");

            if (String.IsNullOrEmpty(OauthRepository.Get().accessToken))
                return RedirectToAction("Index", "Oauth");

            return View(myHomeViewModel);
        }

        public IActionResult Mute()
        {
            myHub.SendCommand(j64Config.VolumeDevice, "mute", "press");
            return View("Index", myHomeViewModel);
        }

        [HttpGet("VolumeLevel/{level}")]
        public IActionResult VolumeLevel(int level)
        {
            myHub.SetVolume(level, previousLevel, j64Config.VolumeDevice);
            return View("Index", myHomeViewModel);
        }

        [HttpGet("SetChannel/{channel}")]
        public IActionResult SetChannel(string channel)
        {
            myHub.ChangeChannel(channel, j64Config.ChannelDevice, j64Config.ChanneKeyPauseInterval);
            return View("Index", myHomeViewModel);
        }

        [HttpGet("Transport/{command}")]
        public IActionResult Transport(string command)
        {
            myHub.SendCommand(j64Config.ChannelDevice, command, "press");
            return View("Index", myHomeViewModel);
        }

        public IActionResult StartSurf()
        {
            myHub.StartChannelSurf(j64Config.ChannelDevice);
            return View("Index", myHomeViewModel);
        }

        public IActionResult StopSurf()
        {
            myHub.StopChannelSurf();
            return View("Index", myHomeViewModel);
        }

        public IActionResult CustomCommand(string command)
        {
            var customCommand = j64Config.CustomCommands.Find(x => x.CommandName == command.Replace("+", " "));
            if (customCommand == null)
                throw new Exception("Invalid command");

            int pause = 0;
            foreach (var action in customCommand.Actions)
            {
                System.Threading.Thread.Sleep(pause);
                myHub.SendCommand(action.Device, action.Function, action.Command);

                // Send a release just in case!
                System.Threading.Thread.Sleep(100);
                myHub.SendCommand(action.Device, action.Function, "release");

                pause = j64Config.ChanneKeyPauseInterval;
            }

            return View("Index", myHomeViewModel);
        }
    }
}
