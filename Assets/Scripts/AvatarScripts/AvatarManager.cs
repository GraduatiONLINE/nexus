﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Normal.Realtime;
using UnityEditor;

namespace Michsky.UI.ModernUIPack {
    public class AvatarManager : MonoBehaviour {
        private Realtime _realtime;

        public ModalWindowManager welcomeWindow;
        public ModalWindowManager settingsWindow;
        public ModalWindowManager adminPanelWindow;
        public NotificationManager connectedNotification;
        public NotificationManager eventStartingNotification;
        private EventManager eventManager;
        private GameObject localPlayer;

        public GameObject trees;

        //public AudioMixer audioMixer;

        public ResolutionSelector resolutionSelector;
        Resolution[] resolutions;
        UnityEvent[] onResolutionChanges;

        public GameObject MainCamera;
        public GameObject FallBackCamera;

        public GameObject ReconnectUI;

        private bool isConnected;


        private void Awake() {
            // Get the Realtime component on this game object
            _realtime = GetComponent<Realtime>();

            // Notify us when Realtime successfully connects to the room
            _realtime.didConnectToRoom += DidConnectToRoom;
            _realtime.didDisconnectFromRoom += DidDisconnectFromRoom;

            _spawn = GameObject.Find("Spawn").transform;

        }

        public void ConnectToRoom() {
            _realtime.Connect("The Circle");
        }

        public void ForceDisconnect () {
            _realtime.Disconnect();
        }
        private void Start()
        {
            eventManager = GameObject.Find("EventManager").GetComponent<EventManager>();
            eventManager.OnEventsChange.AddListener(ReactToEvent);

            resolutions = Screen.resolutions;
            ResolutionSelector.resolutions = resolutions;
            List<ResolutionSelector.Item> resolutionOptions = new List<ResolutionSelector.Item>();

            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width.ToString() + " x " + resolutions[i].height.ToString();
                ResolutionSelector.Item item = new ResolutionSelector.Item(option);
                resolutionOptions.Add(item);

                if (resolutions[i].width == Screen.currentResolution.width
                    && resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionSelector.itemList = resolutionOptions;
            resolutionSelector.defaultIndex = currentResolutionIndex;
            resolutionSelector.index = currentResolutionIndex;
        }

        private bool DidConnect;

        private Transform _spawn;
        private void DidConnectToRoom(Realtime realtime) {
            // Debug.Log("DID CONNECT");

            MainCamera.SetActive(true);
            FallBackCamera.SetActive(false);
            ReconnectUI.SetActive(false);
            // Instantiate the CubePlayer for this client once we've successfully connected to the room
            localPlayer = Realtime.Instantiate("NexusAvatar",                 // Prefab name
                                position: _spawn.position,          // Start 1 meter in the air
                                rotation: _spawn.rotation, // No rotation
                           ownedByClient: true,                // Make sure the RealtimeView on this prefab is owned by this client
                preventOwnershipTakeover: true,                // Prevent other clients from calling RequestOwnership() on the root RealtimeView.
                             useInstance: realtime);           // Use the instance of Realtime that fired the didConnectToRoom event.
            ShowWelcomeWindow();

            nmrLoadingReconnectTrial = 0;
        }

        private int nmrReconnectTrial = 0;
        private float lastReconnectTrial = 0;

        private float startedConnecting = -1;

        private int nmrLoadingReconnectTrial = 0;

        void Update () {
            
                if (nmrReconnectTrial >= 5) {
                    Debug.Log("Not trying again");
                    return;
                }

                if (_realtime.disconnected && !_realtime.connecting && Time.time > lastReconnectTrial + 3 * nmrReconnectTrial) {
                    nmrReconnectTrial += 1;

                    lastReconnectTrial = Time.time;
                    Debug.Log("TRYING TO CONNECT");
                    _realtime.Connect("The Circle");
                }

                if(nmrLoadingReconnectTrial > 10){
                    Application.Quit();
                }

                if(_realtime.connecting){
                    if (startedConnecting < 0) {
                        // Debug.Log("Started CONNECTING");
                        startedConnecting = Time.time;
                        nmrLoadingReconnectTrial += 1;
                    }

                    if (Time.time > startedConnecting + 5.0f * nmrLoadingReconnectTrial) {
                        // Debug.Log("FORCE DISCONNECT");
                        startedConnecting = -1;
                        ForceDisconnect();
                    }
                }
                
            }
        private void DidDisconnectFromRoom(Realtime realtime){
            
            if(MainCamera){
                MainCamera.transform.parent = null;
                MainCamera.SetActive(false);
            }
            if(FallBackCamera){
            FallBackCamera.SetActive(true);
            }
            if(ReconnectUI){
            ReconnectUI.SetActive(true);
            }

            nmrReconnectTrial = 0;
            lastReconnectTrial = 0;
            startedConnecting = -1;

            FallBackCamera.transform.position = MainCamera.transform.position;
            FallBackCamera.transform.rotation = MainCamera.transform.rotation;
            
            if(localPlayer){
                _spawn.position = localPlayer.transform.position;
                _spawn.rotation = localPlayer.transform.rotation;
            }

        }

        public void ShowWelcomeWindow()
        {
            welcomeWindow.OpenWindow();
        }

        public void ShowConnectedNotification()
        {
            connectedNotification.OpenNotification();
        }

        public void ReactToEvent()
        {
            if (eventManager.GetEvents() == null) return;
            if (eventManager.GetEvents()[1] == '1')
            {
                eventStartingNotification.OpenNotification();
            }
            else if (eventManager.GetEvents()[1] == '0')
            {
                eventStartingNotification.CloseNotification();
            }
        }

        public void ShowSettingsWindow()
        {
            settingsWindow.OpenWindow();
        }

        public void ShowAdminPanelWindow()
        {
            adminPanelWindow.OpenWindow();
        }

        public void SetGraphicsSetting(int settingIndex)
        {
            Debug.Log(settingIndex);
            QualitySettings.SetQualityLevel(settingIndex);
            if(settingIndex >= 2)
            {
                ToggleTrees();
            }
        }

        public void SetVolume(float volume)
        {
            //audioMixer.SetFloat("Volume", volume);
        }

        public void ToggleTrees()
        {
            trees.SetActive(!trees.activeSelf);
        }

        public void Respawn()
        {
            // Realtime.Destroy(localPlayer.GetComponent<RealtimeView>());
            _realtime.Disconnect();
            Destroy(localPlayer);
            //GameObject.Find("Realtime").GetComponent<Realtime>().Disconnect();
            Loading.sceneString = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene("Loading");
        }

        public void QuitGame()
        {
            // Realtime.Destroy(localPlayer.GetComponent<RealtimeView>());
            _realtime.Disconnect();
            Destroy(localPlayer);
            //GameObject.Find("Realtime").GetComponent<Realtime>().Disconnect();
            Application.Quit();
        }
    }
}
