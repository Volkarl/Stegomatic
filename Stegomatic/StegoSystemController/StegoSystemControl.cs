﻿using StegomaticProject.StegoSystemModel;
using System.Drawing;
using StegomaticProject.StegoSystemUI;
using System;
using StegomaticProject.StegoSystemUI.Events;
using StegomaticProject.StegoSystemUI.Config;
using StegomaticProject.CustomExceptions;
using System.ComponentModel;

namespace StegomaticProject.StegoSystemController
{
    public class StegoSystemControl //: IStegoSystemControl
    {
        private IStegoSystemModel _stegoModel;
        private IStegoSystemUI _stegoUI;
        private IVerifyUserInput _verifyUserInput;

        public StegoSystemControl(IStegoSystemModel stegoModel, IStegoSystemUI stegoUI)
        {
            this._stegoModel = stegoModel;
            this._stegoUI = stegoUI;
            this._verifyUserInput = new VerifyUserInput();
            _stegoUI.ImageCapacityCalculator = _stegoModel.CalculateImageCapacity;

            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            _stegoUI.NotifyUser += new DisplayNotificationEventHandler(this.ShowNotification);
            _stegoUI.EncodeBtn += new BtnEventHandler(this.EncodeImage);
            _stegoUI.DecodeBtn += new BtnEventHandler(this.DecodeImage);
            //_stegoUI.SaveImageBtn += new BtnEventHandler(this.SaveImage); // MAYBE WE DON'T NEED THIS ONE??
            _stegoUI.OpenImageBtn += new BtnEventHandler(this.OpenImage);

            
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += new DoWorkEventHandler(ThreadedEncode);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ThreadedEncodeComplete);
        }

        BackgroundWorker worker = new BackgroundWorker();

        Bitmap GlobalBitmap = null;

        public void ShowNotification(DisplayNotificationEvent e)
        {
            _stegoUI.ShowNotification(e.Notification, e.Title);
        }

        private void ShowEncodingSuccessNotification(bool encrypt, string encryptionKey, string stegoSeed)
        {
            string notification = string.Empty;
            notification = "Message encoded successfully. \n";
            if (encrypt)
            {
                notification += $"EncryptionKey = {encryptionKey}\n";
            }
            notification += $"StegoSeed = {stegoSeed}";

            _stegoUI.ShowNotification(notification, "Success");
        }

        private void ShowDecodingSuccessNotification(string message)
        {

            message = message.TrimEnd('\0');
            _stegoUI.ShowNotification($"Message decoded successfully: \n \"{message}\"", "Success");
            



            Console.WriteLine(message);
        }

        public void OpenImage(BtnEvent e)
        {
            try
            {
                _stegoUI.OpenImage();
            }
            catch (NotifyUserException exception)
            {
                ShowNotification(new DisplayNotificationEvent(exception));
            }
        }
        
        private void ThreadedEncode(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            Tuple<Bitmap, string, string, string, bool, bool> arg = e.Argument as Tuple<Bitmap, string, string, string, bool, bool>;

            Bitmap coverImage = arg.Item1;
            String message = arg.Item2;
            string encryptionKey = arg.Item3;
            string stegoSeed = arg.Item4;
            bool encrypt = arg.Item5;
            bool compress = arg.Item6;

            e.Result = (Bitmap)_stegoModel.EncodeMessageInImage(coverImage, message, encryptionKey, stegoSeed, encrypt, compress);
        }

        public void ThreadedEncodeComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            GlobalBitmap = (Bitmap)e.Result;
        }

        public void EncodeImage(BtnEvent e)
        {
            try
            {
                IConfig config = _stegoUI.Config;
                string message = _stegoUI.Message;
                Bitmap coverImage = _stegoUI.DisplayImage;
                string encryptionKey = string.Empty;

                _verifyUserInput.Image(coverImage);
                if (config.Encrypt)
                {
                    encryptionKey = _stegoUI.GetEncryptionKey();
                    encryptionKey = _verifyUserInput.EncryptionKey(encryptionKey);
                }
                string stegoSeed = _stegoUI.GetStegoSeed();

                message = _verifyUserInput.Message(message);
                stegoSeed = _verifyUserInput.StegoSeed(stegoSeed);

                //Bitmap stegoObject = _stegoModel.EncodeMessageInImage(coverImage, message, encryptionKey, stegoSeed, config.Encrypt, config.Compress);

                //Tuple<Bitmap, string, string, string, bool, bool> tuple = new Tuple<Bitmap, string, string, string, bool, bool>(coverImage, message, encryptionKey, stegoSeed, config.Encrypt, config.Compress);

                var args = Tuple.Create<Bitmap, string, string, string, bool, bool>(coverImage, message, encryptionKey, stegoSeed, config.Encrypt, config.Compress);

                worker.RunWorkerAsync(args);

                //Prompt to savedialog
                try
                {
                    _stegoUI.SaveImage(GlobalBitmap);
                }
                catch (NotifyUserException exception)
                {
                    ShowNotification(new DisplayNotificationEvent(exception));
                }

                _stegoUI.SetDisplayImage(GlobalBitmap);
                ShowEncodingSuccessNotification(config.Encrypt, encryptionKey, stegoSeed);
            }
            catch (NotifyUserException exception)
            {
                ShowNotification(new DisplayNotificationEvent(exception /* ADD STACK TRACE?? */));
            }
        }

        public void DecodeImage(BtnEvent btnEvent)
        {
            try
            {
                IConfig config = _stegoUI.Config;
                Bitmap coverImage = _stegoUI.DisplayImage;
                string encryptionKey = string.Empty;

                _verifyUserInput.Image(coverImage);
                if (config.Encrypt)
                {
                    encryptionKey = _stegoUI.GetEncryptionKey();
                    encryptionKey = _verifyUserInput.EncryptionKey(encryptionKey);
                }
                string stegoSeed = _stegoUI.GetStegoSeed();
                stegoSeed = _verifyUserInput.StegoSeed(stegoSeed);

                string message = _stegoModel.DecodeMessageFromImage(coverImage, encryptionKey, stegoSeed, config.Encrypt, config.Compress);
                ShowDecodingSuccessNotification(message);
            }
            catch (NotifyUserException exception)
            {
                ShowNotification(new DisplayNotificationEvent(exception.Message, exception.Title));
            }
            catch (AbortActionException)
            {
            }
        }

        //public void SaveImage(BtnEvent btnEvent)
        //{
        //    try
        //    {
        //        _stegoUI.SaveImage();
        //    }
        //    catch (NotifyUserException exception)
        //    {
        //        ShowNotification(new DisplayNotificationEvent(exception));
        //    }
        //}
    }
}