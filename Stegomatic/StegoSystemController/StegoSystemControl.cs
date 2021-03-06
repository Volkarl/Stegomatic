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
    public class StegoSystemControl : IStegoSystemControl
    {
        private IStegoSystemModel _stegoModel;
        private IStegoSystemUI _stegoUI;
        private IVerifyUserInput _verifyUserInput;

        ProcessingPopup _backgroundWorkerProgressBar = new ProcessingPopup();
        private BackgroundWorker _encodeWorker = new BackgroundWorker();
        private BackgroundWorker _decodeWorker = new BackgroundWorker();

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
            _stegoUI.OpenImageBtn += new BtnEventHandler(this.OpenImage);

            SubscribeBackgroundWorkerEvents();
        }

        private void SubscribeBackgroundWorkerEvents()
        {
            //Backgroundworker for encoding
            _encodeWorker.DoWork += new DoWorkEventHandler(ThreadedEncode);
            _encodeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ThreadedEncodeComplete);

            //Backgroundworker for decoding
            _decodeWorker.DoWork += new DoWorkEventHandler(ThreadedDecode);
            _decodeWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ThreadedDecodeComplete);
        }

        public void ShowNotification(DisplayNotificationEvent e)
        {
            _stegoUI.ShowNotification(e.Notification, e.Title);
        }

        private void ShowEncodingSuccessNotification(string encryptionKey, string stegoSeed, bool encrypt, bool compress)
        {
            string notification = string.Empty;
            notification = "Message successfully encoded. \n";
            if (compress)
            {
                notification += "Compressed \n";
            }
            if (encrypt)
            {
                notification += $"EncryptionKey = {encryptionKey} \n";
            }
            notification += $"Password = {stegoSeed}";

            _stegoUI.ShowNotification(notification, "Success");
        }

        private void ShowDecodingSuccessNotification(string message)
        {
            message = message.TrimEnd('\0');
            _stegoUI.ShowMessageNotification($"Message decoded successfully:", message, "Success");
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
            Tuple<Bitmap, string, string, string, bool, bool> EncodingArgument = e.Argument as Tuple<Bitmap, string, string, string, bool, bool>;

            Bitmap coverImage = EncodingArgument.Item1;
            String message = EncodingArgument.Item2;
            string encryptionKey = EncodingArgument.Item3;
            string stegoSeed = EncodingArgument.Item4;
            bool encrypt = EncodingArgument.Item5;
            bool compress = EncodingArgument.Item6;

            try
            {
                Bitmap stegoObject = _stegoModel.EncodeMessageInImage(coverImage, message, encryptionKey, stegoSeed, encrypt, compress);
                var EncodingInfo = new Tuple<Bitmap, string, string, bool, bool>(stegoObject, encryptionKey, stegoSeed, encrypt, compress);
                e.Result = EncodingInfo;
            }
            catch (NotifyUserException exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public void ThreadedEncodeComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            _backgroundWorkerProgressBar.Hide();
            _stegoUI.Enable = true;

            if (e.Error != null)
            {
                ShowNotification(new DisplayNotificationEvent(e.Error.Message, "Error"));
                return;
            }

            var EncodingInfo = e.Result as Tuple<Bitmap, string, string, bool, bool>;
            Bitmap stegoObject = EncodingInfo.Item1;
            
            try
            {
                _stegoUI.SaveImage(stegoObject);
                _stegoUI.SetDisplayImage(stegoObject);
                ShowEncodingSuccessNotification(EncodingInfo.Item2, EncodingInfo.Item3, EncodingInfo.Item4, EncodingInfo.Item5);
            }
            catch (NotifyUserException exception)
            {
                ShowNotification(new DisplayNotificationEvent(exception));
            }
        }

        private void ThreadedDecode(object sender, DoWorkEventArgs e)
        {
            try
            {
                Tuple<Bitmap, string, string, bool, bool> DecodeArgument = e.Argument as Tuple<Bitmap, string, string, bool, bool>;

                Bitmap coverImage = DecodeArgument.Item1;
                string encryptionKey = DecodeArgument.Item2;
                string stegoSeed = DecodeArgument.Item3;
                bool encrypt = DecodeArgument.Item4;
                bool compress = DecodeArgument.Item5;

                string message = _stegoModel.DecodeMessageFromImage(coverImage, encryptionKey, stegoSeed, encrypt, compress);

                e.Result = message;
            }
            catch (NotifyUserException exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public void ThreadedDecodeComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            _backgroundWorkerProgressBar.Hide();
            _stegoUI.Enable = true;

            if (e.Error != null)
            {
                ShowNotification(new DisplayNotificationEvent(e.Error.Message, "Error"));
                return;
            }

            string message = (string)e.Result;
            ShowDecodingSuccessNotification(message);
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

                var args = Tuple.Create<Bitmap, string, string, string, bool, bool>(coverImage, message, encryptionKey, stegoSeed, config.Encrypt, config.Compress);

                _backgroundWorkerProgressBar.Show(); 
                // Show that we're working on it!

                _stegoUI.Enable = false; 
                // Disable the main-window, so the user click on anything they're not supposed to.

                _encodeWorker.RunWorkerAsync(args);
                // When worker is done and event will fire and ThreadedEncodeComplete() will be executed, which
                // will start a save-dialog when the encoding-process is completed.
            }
            catch (NotifyUserException exception)
            {
                ShowNotification(new DisplayNotificationEvent(exception));
            }
            catch (AbortActionException)
            {
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

                _backgroundWorkerProgressBar.Text = "Decoding...";
                _backgroundWorkerProgressBar.label1.Text = "Extracting message from image...";
                _backgroundWorkerProgressBar.Show();
                // Show that we're working on it!

                _stegoUI.Enable = false;
                // Disable the main-window, so the user click on anything they're not supposed to.

                var args = Tuple.Create<Bitmap, string, string, bool, bool>(coverImage, encryptionKey, stegoSeed, config.Encrypt, config.Compress);

                _decodeWorker.RunWorkerAsync(args);
            }
            catch (NotifyUserException exception)
            {
                ShowNotification(new DisplayNotificationEvent(exception.Message, exception.Title));
            }
            catch (AbortActionException)
            {
            }
        }
    }
}