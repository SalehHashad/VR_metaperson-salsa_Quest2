﻿using UnityEngine;

namespace Crosstales.RTVoice.Demo.Util
{
   /// <summary>Controls the custom provider in demo builds.</summary>
   [HelpURL("https://www.crosstales.com/media/data/assets/rtvoice/api/class_crosstales_1_1_r_t_voice_1_1_demo_1_1_util_1_1_custom_provider_controller.html")]
   public class CustomProviderController : MonoBehaviour
   {
      #region Variables

      public Crosstales.RTVoice.Provider.BaseCustomVoiceProvider Provider;

      //[Header("WebGL")]
      //public bool KeepOnDestroy = false;
      public bool ParentProvider = false;

      private bool isCustom;
      //private Crosstales.RTVoice.Provider.BaseCustomVoiceProvider previousProvider;

      #endregion


      #region MonoBehaviour methods

      private void Start()
      {
         isCustom = Speaker.Instance.CustomMode;

         if (Provider != null)
         {
            //isCustom = Speaker.Instance.CustomMode;
            //previousProvider = Speaker.Instance.CustomProvider;

            Speaker.Instance.CustomProvider = Provider;
            Speaker.Instance.CustomMode = true;

            //if (Crosstales.RTVoice.Util.Helper.isWebGLPlatform && KeepOnDestroy)
            if (ParentProvider)
            {
               /*
               for (int ii = Speaker.Instance.transform.childCount - 1; ii >= 0; ii--)
               {
                  Transform child = Speaker.Instance.transform.GetChild(ii);
                  child.SetParent(null);
                  //Destroy(child.gameObject);
               }
               */
               Provider.transform.SetParent(Speaker.Instance.transform);
            }
         }
      }

      private void OnDestroy()
      {
         if (Speaker.Instance != null)
         {
            Speaker.Instance.CustomMode = isCustom;

            //if (!Crosstales.RTVoice.Util.Helper.isWebGLPlatform || !KeepOnDestroy)
            /*
            if (ParentProvider)
            {
               Speaker.Instance.CustomMode = isCustom;

               if (previousProvider != null)
               {
                  Speaker.Instance.CustomProvider = previousProvider;
                  //Provider.transform.SetParent(null);
               }
            }
            */
         }
      }

      #endregion
   }
}
// © 2020-2024 crosstales LLC (https://www.crosstales.com)