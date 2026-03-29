using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterCounterOffer
{
    public static class Utility
    {
        public static void PrintException(Exception e)
        {
            MelonLogger.Msg(ConsoleColor.Red, e.Source);
            MelonLogger.Msg(ConsoleColor.DarkRed, e.Message);
        }

        public static void Error(string msg)
        {
            MelonLogger.Msg(ConsoleColor.Red, msg);
        }

        public static void Log(string msg)
        {
            MelonLogger.Msg(ConsoleColor.DarkMagenta, msg);
        }

        public static void Success(string msg)
        {
            MelonLogger.Msg(ConsoleColor.Green, msg);
        }

        public static void ListHierarchy(Transform transform, int depth)
        {
            string indent = new string(' ', depth * 2);
            MelonLogger.Msg($"{indent}- {transform.name}");
            foreach (Transform child in transform)
            {
                ListHierarchy(child, depth + 1);
            }
        }

    }
}