using LT.Logger;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

namespace LT_Education
{
    public partial class LT_EducationBehaviour : CampaignBehaviorBase
    {


        public static void CreatePopupVMLayer(string title, string smallText, string bigText, string textOverImage, string spriteName, string closeButtonText)
        {
            try
            {
                bool flag = _gauntletLayer != null;
                if (!flag)
                {
                    _gauntletLayer = new GauntletLayer("GauntletLayer", 1000, false);

                    bool flag2 = _popupVM == null;
                    if (flag2)
                    {
                        // Localization
                        TextObject titleTO = new(title);
                        TextObject smallTextTO = new(smallText);
                        TextObject bigTextTO = new(bigText);
                        TextObject textOverImageTO = new(textOverImage);
                        TextObject closeButtonTextTO = new(closeButtonText);

                        _popupVM = new EducationPopupVM(titleTO.ToString(), smallTextTO.ToString(), bigTextTO.ToString(), textOverImageTO.ToString(), spriteName, closeButtonTextTO.ToString());
                    }
                    _gauntletMovie = _gauntletLayer.LoadMovie("LTEducationBookPopup", _popupVM);
                    _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
                    _popupScreen = ScreenManager.TopScreen;
                    _popupScreen.AddLayer(_gauntletLayer);
                    _gauntletLayer.IsFocusLayer = true;
                    ScreenManager.TrySetFocus(_gauntletLayer);
                    if (_popupVM != null) _popupVM.Refresh();
                }
            }
            catch (Exception ex)
            {
                LTLogger.LogError(ex);
            }
        }

        public static void DeletePopupVMLayer()
        {
            if (_gauntletLayer != null)
            {
                _gauntletLayer.InputRestrictions.ResetInputRestrictions();
                _gauntletLayer.IsFocusLayer = false;
                bool flag2 = _gauntletMovie != null;
                if (flag2)
                {
                    _gauntletLayer.ReleaseMovie(_gauntletMovie);
                }
                // remove from the screen the layer was added to, which may no longer be the top screen
                _popupScreen?.RemoveLayer(_gauntletLayer);
            }

            ResetPopupState();
        }

        private static void ResetPopupState()
        {
            _gauntletLayer = null;
            _gauntletMovie = null;
            _popupVM = null;
            _popupScreen = null;
        }

    }
}
