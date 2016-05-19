﻿using Kinect2Server.View;
using Microsoft.Speech.Recognition;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using ZeroMQ;

namespace Kinect2Server
{
    public class NetworkResponder
    {
        private ZContext context;
        private ZSocket socket;
        private Boolean binded;
        private Thread json_thread;
        private MainWindow mw;
        private TextToSpeech tts;
        private SpeechRecognition sr;
        private SkeletonTracking st;
        private SpeechRecognitionView srv;

        public NetworkResponder()
        {
            this.mw = (MainWindow)Application.Current.MainWindow;
            this.tts = this.mw.TextToSpeech;
            this.sr = this.mw.SpeechRecogniton;
            this.st = this.mw.SkeletonTracking;
            this.context = new ZContext();
            this.socket = new ZSocket(this.context, ZSocketType.REP);
            this.binded = false;
            this.srv = this.mw.SpeechRecognitionView;
            this.json_thread = new Thread(new ThreadStart(this.ReceiveJson));
            this.json_thread.SetApartmentState(ApartmentState.STA);
            this.json_thread.IsBackground = true;
            this.json_thread.Start();
        }

        public void Bind(String listeningPort)
        {
            String status = null;
            try
            {
                this.socket.Bind("tcp://*:" + listeningPort);
                this.binded = true;
            }
            catch (ZException e)
            {
                status = ("Socket connection failed, server cannot listen on port " + listeningPort + ": " + e.Message);
            }
        }

        public void ReceiveJson()
        {
            while (Thread.CurrentThread.IsAlive)
            {
                String status = null;
                if (this.binded)
                {
                    try 
                    {
                        ZFrame frame = this.socket.ReceiveFrame();
                        status = frame.ReadString();
                        String reply="Reply:";

                        //Params time bitches
                        JObject parameters = JObject.Parse(status);
                        Boolean hasChanged = false;
                        //Speech Recognition
                        Nullable<Boolean> on = (Nullable<Boolean>)parameters["speech_recognition"]["on"];
                        String grammarFile = (String)parameters["speech_recognition"]["file_name"];
                        String grammar = (String)parameters["speech_recognition"]["grammar"];
                        if (on!=null) {
                            if (this.sr.isSpeechEngineSet())
                            {
                                if ((Boolean)on && !hasChanged)
                                {
                                    this.sr.SpeechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
                                    this.RefreshStatus("speech",true);
                                    hasChanged = true;
                                }

                                else
                                {
                                    this.sr.SpeechRecognitionEngine.RecognizeAsyncStop();
                                    this.RefreshStatus("speech", false);
                                }
                                    
                            }
                            else if (grammar==null)
                            {
                                reply += " Speech engine not set, you have to send grammar file.";
                            }
                        }
                        if (grammar != null)
                        {
                            this.sr.createGrammar(null, null, grammar);
                            this.srv.addlist();
                            this.sr.FileName = grammarFile;
                         }
                        Nullable<double> confidence = (Nullable<double>)parameters["speech_recognition"]["confidence"];
                        if (confidence != null && confidence != this.sr.ConfidenceThreshold)
                        {
                            this.sr.ConfidenceThreshold = (double)confidence;
                            this.srv.confidenceSelector.Value = (int)this.sr.ConfidenceThreshold * 100;
                        }
                            
                        else if (confidence == this.sr.ConfidenceThreshold)
                            reply += " Confidence is the same";
                        Nullable<Boolean> sentence = (Nullable<Boolean>)parameters["speech_recognition"]["sentence"];
                        if (sentence != null)
                            this.sr.SentenceStatus = (Boolean)sentence;
                        Nullable<Boolean> semantic = (Nullable<Boolean>)parameters["speech_recognition"]["semantic"];
                        if (semantic != null)
                            this.sr.SemanticsStatus = (Boolean)semantic;
                        //Skeleton Tracking
                        on = (Nullable<Boolean>)parameters["skeleton_tracking"]["on"];
                        if (on != null)
                        {
                            if ((Boolean)on)
                                this.st.BodyFrameReader.IsPaused = false;
                            else
                                this.st.BodyFrameReader.IsPaused = true;
                        }
                        Nullable<float> smoothing = (Nullable<float>)parameters["skeleton_tracking"]["smoothing"];
                        if (smoothing != null && smoothing != this.st.SmoothingParam)
                            this.st.SmoothingParam = (float)smoothing;
                        else if (smoothing == this.st.SmoothingParam)
                            reply += " Smoothing is the same";
                        //Text To Speech
                        Nullable<Boolean> queue = (Nullable<Boolean>)parameters["text_to_speech"]["queue"];
                        if (queue != null)
                        {
                            if ((Boolean)queue)
                                this.tts.QueuedMessages = true;
                            else
                                this.tts.QueuedMessages = false;
                        }
                        String gender = (String)parameters["text_to_speech"]["gender"];
                        if (gender != null)
                        {
                            if (gender.Equals("male"))
                                this.tts.VoiceGender = VoiceGender.Male;
                            else if (gender.Equals("female"))
                                this.tts.VoiceGender = VoiceGender.Female;
                        }
                        String language = (String)parameters["text_to_speech"]["language"];
                        if (language != null)
                        {
                            if (language.Equals("english"))
                                this.tts.Culture = new CultureInfo("en-US");
                            else if (language.Equals("french"))
                                this.tts.Culture = new CultureInfo("fr-FR");
                        }

                        ZFrame perfect = new ZFrame("Parameters updated");
                        this.socket.Send(perfect);
                    }
                    catch (ZException e)
                    {
                        status = "Cannot receive message: " + e.Message;
                    }
                }
                else
                {
                    status = "Cannot receive message: Not connected";
                }
            }
        }

        private void RefreshStatus(String feature,Boolean state)
        {
            if (feature.Equals("speech"))
            {
                if (state)
                {
                    this.srv.setButtonOn(this.srv.stackSR);
                }
                else
                {
                    this.srv.setButtonOff(this.srv.stackSR);
                }
            }
        }

        public void Close()
        {
            this.socket.Close();
            this.binded = false;
        }

        ~NetworkResponder()
        {
            this.context.Dispose();
        }
    }
}
