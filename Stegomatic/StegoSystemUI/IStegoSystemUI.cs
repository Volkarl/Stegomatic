﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StegomaticProject.StegoSystemUI.Config;
using StegomaticProject.StegoSystemUI.Events;

namespace StegomaticProject.StegoSystemUI
{
    public interface IStegoSystemUI
    {
        // Get info from UI
        string Message { get; }
        string PathOfCoverImage { get; }
        IConfig Config { get; }
        Bitmap DisplayImage { get; }

        // Modify UI
        void SetDisplayImage(Bitmap newImage);
        void ShowNotification(string notification);
        string GetEncryptionKey();
        string GetStegoSeed();

        // Start/End
        void Start();
        void Terminate();

        // Events
        event DisplayNotificationEventHandler NotifyUser;
        event BtnEventHandler DecodeImageBtn;
        event BtnEventHandler EncodeImageBtn;
        event BtnEventHandler SaveImageBtn;
    }
}
