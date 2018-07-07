﻿using Spectrum.API;
using Spectrum.API.Interfaces.Plugins;
using Spectrum.API.Interfaces.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Spectrum.API.Configuration;
using System.IO;
using Harmony;
using System.Reflection;
using Events.Car;
using Events.LocalClient;
using Events;
using Events.Stunt;
using Events.Local;
using Events.ClientToAllClients;
using Spectrum.API.Experimental;

namespace CustomCar
{
    public class Configs
    {
        public Configs()
        {
            var settings = new Settings("CustomCar");
        }
    }

    public class Entry : IPlugin
    {
        const string carName = "assets/delorean.prefab";

        static System.Random rnd = new System.Random();
        static Configs configs;
        static Spectrum.API.Logging.Logger logger;
        static Assets assets;
        static GameObject car;

        public void Initialize(IManager manager, string ipcIdentifier)
        {
            Console.Out.WriteLine(Application.unityVersion);
            configs = new Configs();
            logger = new Spectrum.API.Logging.Logger("CustomCar");

            var harmony = HarmonyInstance.Create("com.Larnin.CustomCar");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            try
            {
                assets = new Assets("Delorean");
                car = assets.Bundle.LoadAsset<GameObject>(carName);
            }
            catch(Exception e)
            {
                Console.Out.WriteLine(e.Message);
                Console.Out.WriteLine(e.ToString());
            }
        }

        [HarmonyPatch(typeof(CarLogic), "Awake")]
        internal class CarLogicAwake
        {

            static void Postfix(CarLogic __instance)
            {
                Entry.logCurrentObjectAndChilds(__instance.gameObject, true);

                try
                {
                    var asset = GameObject.Instantiate(car);
                    asset.transform.parent = __instance.transform;
                    asset.transform.localPosition = Vector3.zero;
                    asset.transform.localRotation = Quaternion.identity;
                    asset.SetLayerRecursively(__instance.gameObject.GetLayer());
                    asset.GetComponentInChildren<MeshRenderer>().material = new Material(Shader.Find("Diffuse"));
                    logCurrentObjectAndChilds(asset, false);
                    logger.WriteLine(asset.transform.parent.name);

                    var mat = new Material(Shader.Find("Diffuse"));

                    foreach (var renderer in GameObject.FindObjectsOfType<Renderer>())
                    {
                        renderer.material = mat;
                    }

                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                    Console.Out.WriteLine(e.ToString());
                }

                Entry.logCurrentObjectAndChilds(__instance.gameObject, false);
            }
        }
        
        static void logCurrentObjectAndChilds(GameObject obj, bool hideStuff, int level = 0)
        {
            const int offsetPerLevel = 2;
            string offset = new string(' ', level * offsetPerLevel);
            
            logger.WriteLine(offset + obj.GetType().Name + " - " + obj.name + " - " + obj.activeSelf + " - " + obj.transform.localPosition + " - " + obj.transform.localRotation.eulerAngles + " - " + obj.transform.localScale + " - " + obj.layer);
            foreach (var comp in obj.GetComponents(typeof(Component)))
            {
                string text = offset + "   | " + comp.GetType().Name;
                var behaviour = comp as Behaviour;
                if (behaviour != null)
                {
                    text += " - " + behaviour.enabled;
                }
                var renderer = comp as MeshRenderer;
                if(renderer != null)
                {
                    foreach(var m in renderer.materials)
                    {
                        text += " - " + m.name;
                    }
                }
                var filter = comp as MeshFilter;
                if(filter != null)
                {
                    text += " - " + filter.mesh.name + " - " + filter.mesh.GetIndexCount(0) + " - " + filter.mesh.GetTriangles(0).Length;
                }
                logger.WriteLine(text);
                if(hideStuff)
                    disableSpecificObject(comp);
            }
            for (int i = 0; i < obj.transform.childCount; i++)
                logCurrentObjectAndChilds(obj.transform.GetChild(i).gameObject, hideStuff, level + 1);
        }

        static void disableSpecificObject(Component comp)
        {
            var renderer = comp as MeshRenderer;
            var skinnedRenderer = comp as SkinnedMeshRenderer;
            if(renderer != null)
            {
                //if (renderer.name.Contains("Wheel") || renderer.name.Contains("HubVisual") || renderer.name.Contains("JetFlame") || renderer.name.Contains("CarCrossSection") || renderer.name.Contains("Refractor"))
                    renderer.enabled = false;
            }
            if(skinnedRenderer != null)
            {
                skinnedRenderer.enabled = false;
            }
        }
    }
}
