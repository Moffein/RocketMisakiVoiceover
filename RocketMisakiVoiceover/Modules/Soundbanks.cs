﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RocketMisakiVoiceover.Modules
{
    public static class SoundBanks
    {
        private static bool initialized = false;
        public static string SoundBankDirectory
        {
            get
            {
                return Files.assemblyDir;
            }
        }

        public static void Init()
        {
            if (initialized) return;
            initialized = true;
            AKRESULT akResult = AkSoundEngine.AddBasePath(SoundBankDirectory);

            AkSoundEngine.LoadBank("RocketMisakiSoundbank.bnk", out _);
        }
    }
}
