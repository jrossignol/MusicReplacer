using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;

namespace MusicReplacer
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class MusicReplacer : MusicLogic
    {
        public enum Theme
        {
            menuTheme,
            menuAmbience,
            credits,
            trackingAmbience,
            spaceCenterAmbience,
            VABAmbience,
            SPHAmbience,
            astroComplexAmbience,
            researchComplexAmbience,
            missionControlAmbience,
            adminFacilityAmbience,
            constructionPlaylist,
            spacePlaylist,
        }

        public class Replacement
        {
            public Theme theme;
            public AudioClip clip;
            public CelestialBody body;
            public float minAltitude;
            public float maxAltitude = float.MaxValue;

            public override string ToString()
            {
                return this == null || clip == null ? "null" : clip.name;
            }
        }

        public static MusicReplacer Instance;
        public List<Replacement> replacements = new List<Replacement>();
        public List<AudioClip> originalMusic = null;

        protected IEnumerable<Replacement> currentReplacements = Enumerable.Empty<Replacement>();

        public bool loaded = false;
        float lastUpdate = 0.0f;
        bool isInDefaultMode = true;

        double spaceAltitude;
        AudioSource fadeOutAudio = null;

        void Awake()
        {
            if (fetch != null)
            {
                if (fetch.GetType() != typeof(MusicReplacer))
                {
                    Debug.Log("[MusicReplacer] Replacing existing MusicLogic");

                    // Don't know where to find the prefab, so copy from the existing MusicLogic
                    menuTheme = fetch.menuTheme;
                    menuAmbience = fetch.menuAmbience;
                    credits = fetch.credits;
                    trackingAmbience = fetch.trackingAmbience;
                    spaceCenterAmbience = fetch.spaceCenterAmbience;
                    VABAmbience = fetch.VABAmbience;
                    SPHAmbience = fetch.SPHAmbience;
                    astroComplexAmbience = fetch.astroComplexAmbience;
                    researchComplexAmbience = fetch.researchComplexAmbience;
                    missionControlAmbience = fetch.missionControlAmbience;
                    adminFacilityAmbience = fetch.adminFacilityAmbience;
                    constructionPlaylist = fetch.constructionPlaylist;
                    spacePlaylist = fetch.spacePlaylist;
                    audio1 = fetch.audio1;
                    audio2 = fetch.audio2;

                    DestroyImmediate(fetch);
                    fetch = null;
                }
                else
                {
                    Debug.LogWarning("[MusicReplacer] Not creating duplicate MusicReplacer.");
                    DestroyImmediate(this);
                    return;
                }
            }

            Debug.Log("[MusicReplacer] Setting up MusicLogic copy...");
            MethodInfo awakeMethod = typeof(MusicLogic).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awakeMethod.Invoke(this, new object[] { });
            Instance = this;

            Debug.Log("[MusicReplacer] Hooking into loading subsystem...");
            LoadingScreen screen = FindObjectOfType<LoadingScreen>();
            if (screen == null)
            {
                Debug.LogError("[MusicReplacer] Couldn't find LoadingScreen!");
                return;
            }

            List<LoadingSystem> loadList = LoadingScreen.Instance.loaders;
            if (loadList == null)
            {
                Debug.LogError("[MusicReplacer] Couldn't find loaders!");
                return;
            }
        }

        void Update()
        {
            // Would love to do this as a proper loader, but FlightGlobals.Bodies is not initialized then
            if (HighLogic.LoadedScene == GameScenes.MAINMENU && !loaded)
            {
                MusicReplacerLoader.PerformLoad();
                loaded = true;

                spaceAltitude = FlightGlobals.GetHomeBody().atmosphereDepth;

                // The stock prefab has some odd settings:
                //   - Turn doppler off, as this makes things sound weird when the camera zooms or moves
                //   - Change the min/max distance so that we can always hear the music
                audio1.dopplerLevel = 0;
                audio1.minDistance = float.MaxValue;
                audio1.maxDistance = float.MaxValue;
            }
            else if (HighLogic.LoadedScene == GameScenes.FLIGHT && audio1 != null && FlightGlobals.ActiveVessel != null)
            {
                if (lastUpdate + 0.5f < Time.fixedTime)
                {
                    Debug.Log("In update loop (" + audio1.isPlaying + ")");
                    lastUpdate = Time.fixedTime;

                    IEnumerable<Replacement> selections = replacements.Where(r =>
                        r.theme == Theme.spacePlaylist &&
                        (r.body == null || r.body == FlightGlobals.currentMainBody) &&
                        FlightGlobals.ActiveVessel.altitude >= r.minAltitude &&
                        FlightGlobals.ActiveVessel.altitude < r.maxAltitude
                    );

                    bool replacementsChanged = selections.Where(r => !currentReplacements.Contains(r)).Any() ||
                        currentReplacements.Where(r => !selections.Contains(r)).Any();

                    if (replacementsChanged)
                    {
                        Debug.Log("    replacement list has changed");
                        currentReplacements = selections.ToList();
                        spacePlaylist = currentReplacements.Any() ? currentReplacements.Select(r => r.clip).ToList() : originalMusic;
                    }
                    Debug.Log("    first replacement is: " + currentReplacements.FirstOrDefault());

                    if (isInDefaultMode && currentReplacements.Any())
                    {
                        Debug.Log("    switching to non-default mode...");
                        isInDefaultMode = false;
                        RestartMusic();
                    }
                    else if (!isInDefaultMode)
                    {
                        if (!currentReplacements.Any())
                        {
                            Debug.Log("    switching to back to default mode...");
                            isInDefaultMode = true;
                            RestartMusic();
                        }
                        else if (!spacePlaylist.Contains(audio1.clip) && replacementsChanged)
                        {
                            Debug.Log("    no mode change...");
                            RestartMusic();
                        }
                    }

                    if (audio1.clip != null)
                    {
                        Debug.Log("    audio1 = " + audio1.clip.name);
                    }
                }
            }
            else if (HighLogic.LoadedScene != GameScenes.FLIGHT && !isInDefaultMode)
            {
                flightMusicSpaceAltitude = spaceAltitude;
                isInDefaultMode = true;
            }
        }

        protected void RestartMusic(bool crossfade = false)
        {
            Debug.Log("    restarting music...");
            flightMusicSpaceAltitude = isInDefaultMode ? spaceAltitude : 0.0;
            StopAllCoroutines();
            if (audio1.isPlaying)
            {
                StartCoroutine(DoCrossFade(1.0f));
            }
            audio1.Stop();
            StartCoroutine("PlayFlight");
        }

        protected IEnumerator<YieldInstruction> DoCrossFade(float duration)
        {
            if (fadeOutAudio != null)
            {
                fadeOutAudio.Stop();
                Destroy(fadeOutAudio);
                fadeOutAudio = null;
            }

            fadeOutAudio = (AudioSource)UnityEngine.Object.Instantiate(audio1);

            float startTime = Time.time;
            float endTime = startTime + duration;
            while (Time.time < endTime)
            {
                float ratio = (Time.time - startTime) / duration;
                audio1.volume = GameSettings.MUSIC_VOLUME * (1.0f - ratio);
                fadeOutAudio.volume = GameSettings.MUSIC_VOLUME * (ratio);
                yield return null;
            }
            audio1.volume = GameSettings.MUSIC_VOLUME;
            fadeOutAudio.Stop();
            Destroy(fadeOutAudio);
            fadeOutAudio = null;
        }
    }
}
