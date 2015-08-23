using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;

namespace MusicReplacer
{
    public class MusicReplacerLoader
    {
        public static void PerformLoad()
        {
            ConfigNode[] musicNodes = GameDatabase.Instance.GetConfigNodes("MUSIC_REPLACER");
            IEnumerable<ConfigNode> nodes = musicNodes.SelectMany(cn => cn.GetNodes("MUSIC"));

            bool removeConstruction = false;
            bool removeSpace = false;
            int constructionCount = MusicReplacer.Instance.constructionPlaylist.Count;
            int spaceCount = MusicReplacer.Instance.spacePlaylist.Count;

            foreach (ConfigNode node in nodes)
            {
                MusicReplacer.Replacement replacement = new MusicReplacer.Replacement();
                try
                {
                    bool valid = true;

                    if (!node.HasValue("name"))
                    {
                        Debug.LogError("[MusicReplacer] Couldn't load MUSIC node - no name attribute found");
                        valid = false;
                    }
                    else
                    {
                        replacement.theme = (MusicReplacer.Theme)Enum.Parse(typeof(MusicReplacer.Theme), node.GetValue("name"));
                    }

                    if (!node.HasValue("musicURL"))
                    {
                        Debug.LogError("[MusicReplacer] Couldn't load MUSIC node - no musicURL attribute found");
                        valid = false;
                    }
                    else
                    {
                        string url = node.GetValue("musicURL");
                        replacement.clip = GameDatabase.Instance.databaseAudio.Where(a => a.name == url).FirstOrDefault();
                        if (replacement.clip == null)
                        {
                            Debug.LogError("[MusicReplacer] Couldn't find audio file at URL: " + url);
                            valid = false;
                        }
                    }

                    bool removeExisting = false;
                    if (node.HasValue("removeExisting"))
                    {
                        removeExisting = bool.Parse(node.GetValue("removeExisting"));
                    }

                    if (node.HasValue("celestialBody"))
                    {
                        if (replacement.theme != MusicReplacer.Theme.spacePlaylist)
                        {
                            Debug.LogError("[MusicReplacer] The celestialBody attribute is only valid for spacePlaylist.");
                            valid = false;
                        }
                        else
                        {
                            string bodyName = node.GetValue("celestialBody");
                            replacement.body = FlightGlobals.Bodies.Where(cb => cb.name == bodyName).FirstOrDefault();
                            if (replacement.body == null)
                            {
                                Debug.LogError("[MusicReplacer] Couldn't find celestial body named '" + bodyName + "'.");
                                valid = false;
                            }
                        }
                    }

                    if (node.HasValue("minAltitude"))
                    {
                        if (replacement.theme != MusicReplacer.Theme.spacePlaylist)
                        {
                            Debug.LogError("[MusicReplacer] The minAltitude attribute is only valid for spacePlaylist.");
                            valid = false;
                        }
                        else
                        {
                            replacement.minAltitude = (float)Double.Parse(node.GetValue("minAltitude"));
                        }
                    }

                    if (node.HasValue("maxAltitude"))
                    {
                        if (replacement.theme != MusicReplacer.Theme.spacePlaylist)
                        {
                            Debug.LogError("[MusicReplacer] The maxAltitude attribute is only valid for spacePlaylist.");
                            valid = false;
                        }
                        else
                        {
                            replacement.maxAltitude = (float)Double.Parse(node.GetValue("maxAltitude"));
                        }
                    }

                    if (valid)
                    {
                        Debug.Log("[MusicReplacer] Loaded a MUSIC node for " + replacement.theme);

                        // Perform a simple replacement
                        if (replacement.theme != MusicReplacer.Theme.spacePlaylist ||
                            replacement.body == null && replacement.minAltitude == 0.0f && replacement.maxAltitude == float.MaxValue)
                        {
                            switch (replacement.theme)
                            {
                                case MusicReplacer.Theme.menuTheme:
                                    MusicReplacer.Instance.menuTheme = replacement.clip;
                                    break;
                                case MusicReplacer.Theme.menuAmbience:
                                    MusicReplacer.Instance.menuAmbience = replacement.clip;
                                    break;
                                case MusicReplacer.Theme.credits:
                                    MusicReplacer.Instance.credits = replacement.clip;
                                    break;
                                case MusicReplacer.Theme.trackingAmbience:
                                    MusicReplacer.Instance.trackingAmbience = replacement.clip;
                                    break;
                                case MusicReplacer.Theme.spaceCenterAmbience:
                                    MusicReplacer.Instance.spaceCenterAmbience = replacement.clip;
                                    break;
                                case MusicReplacer.Theme.VABAmbience:
                                    MusicReplacer.Instance.VABAmbience = replacement.clip;
                                    break;
                                case MusicReplacer.Theme.SPHAmbience:
                                    MusicReplacer.Instance.SPHAmbience = replacement.clip;
                                    break;
                                case MusicReplacer.Theme.astroComplexAmbience:
                                    MusicReplacer.Instance.astroComplexAmbience = replacement.clip;
                                    break;
                                case MusicReplacer.Theme.researchComplexAmbience:
                                    MusicReplacer.Instance.researchComplexAmbience = replacement.clip;
                                    break;
                                case MusicReplacer.Theme.missionControlAmbience:
                                    MusicReplacer.Instance.missionControlAmbience = replacement.clip;
                                    break;
                                case MusicReplacer.Theme.adminFacilityAmbience:
                                    MusicReplacer.Instance.adminFacilityAmbience = replacement.clip;
                                    break;
                                case MusicReplacer.Theme.constructionPlaylist:
                                    MusicReplacer.Instance.constructionPlaylist.Add(replacement.clip);
                                    break;
                                case MusicReplacer.Theme.spacePlaylist:
                                    MusicReplacer.Instance.spacePlaylist.Add(replacement.clip);
                                    break;
                            }
                        }
                        // Add to our lists
                        else
                        {
                            MusicReplacer.Instance.replacements.Add(replacement);
                        }

                        // Flag the original lists for removal
                        if (replacement.theme == MusicReplacer.Theme.spacePlaylist)
                        {
                            removeSpace |= removeExisting;
                        }
                        else if (replacement.theme == MusicReplacer.Theme.constructionPlaylist)
                        {
                            removeConstruction |= removeExisting;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("[MusicReplacer] Exception trying to load MUSIC node:");
                    Debug.LogException(e);
                }
            }

            // Remove existing values, if required
            if (removeConstruction)
            {
                MusicReplacer.Instance.constructionPlaylist.RemoveRange(0, constructionCount);
            }
            if (removeSpace)
            {
                MusicReplacer.Instance.spacePlaylist.RemoveRange(0, spaceCount);
            }

            // Get the original in-flight list saved
            MusicReplacer.Instance.originalMusic = MusicReplacer.Instance.spacePlaylist.ToList();

            MusicReplacer.Instance.loaded = true;
        }
    }
}
