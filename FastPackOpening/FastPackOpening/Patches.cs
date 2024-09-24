﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection.Emit;
using TMPro;

namespace FastPackOpening
{
    [HarmonyPatch]
    public class Patches
    {
        public static bool IsAutoOpen { get; set; }
        public static bool DelayAutoOpen { get; set; }
        public static bool DelayAutoOpen2 { get; set; }
        public static int PacksInHand { get; set; }
        public static float PackSpeedMultiplier { get; set; }
        public static float LogTimer { get; set; }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InteractionPlayerController), nameof(InteractionPlayerController.EvaluateOpenCardPack))]
        public static bool InteractionPlayerController_EvaluateOpenCardPack_Prefix(ref InteractionPlayerController __instance)
        {
            if (!Plugin.EnableMod.Value)
            {
                __instance.m_OpenCardBoxInnerMesh["OpenCardBoxAnim"].speed = 1f;
                return true;
            }

            __instance.m_OpenCardBoxInnerMesh["OpenCardBoxAnim"].speed = Plugin.SpeedMultiplierValue;

            if (__instance.CanOpenPack())
            {
                Item item = __instance.m_HoldItemList[0];
                __instance.RemoveHoldItem(item);
                CSingleton<CardOpeningSequence>.Instance.ReadyingCardPack(item);
                __instance.m_IsHoldingMouseDown = false;
                __instance.m_IsHoldingRightMouseDown = false;
                return false;
            }
            if (__instance.CanOpenCardBox())
            {
                __instance.m_IsOpeningCardBox = true;
                Item item2 = __instance.m_HoldItemList[0];
                EItemType eitemType = __instance.CardBoxToCardPack(item2.GetItemType());
                if (eitemType == EItemType.None)
                {
                    return false;
                }
                ItemMeshData itemMeshData = InventoryBase.GetItemMeshData(item2.GetItemType());
                __instance.m_OpenCardBoxMeshFilter.mesh = itemMeshData.mesh;
                __instance.m_OpenCardBoxMesh.material = itemMeshData.material;
                item2.gameObject.SetActive(false);
                __instance.m_HoldItemList.Clear();
                CPlayerData.m_HoldItemTypeList.Clear();
                InteractionPlayerController.RemoveToolTip(EGameAction.OpenCardBox);
                SoundManager.PlayAudio("SFX_OpenCardBox", 0.6f, 1f);
                __instance.m_OpenCardBoxInnerMesh.gameObject.SetActive(true);
                __instance.m_OpenCardBoxInnerMesh.Rewind();
                __instance.m_OpenCardBoxInnerMesh.Play();
                for (int i = 0; i < 8; i++)
                {
                    ItemMeshData itemMeshData2 = InventoryBase.GetItemMeshData(eitemType);
                    Item item3 = ItemSpawnManager.GetItem(__instance.m_OpenCardBoxSpawnCardPackPosList[i]);
                    item3.SetMesh(itemMeshData2.mesh, itemMeshData2.material, eitemType, itemMeshData2.meshSecondary, itemMeshData2.materialSecondary);
                    item3.transform.position = __instance.m_OpenCardBoxSpawnCardPackPosList[i].position;
                    item3.transform.rotation = __instance.m_OpenCardBoxSpawnCardPackPosList[i].rotation;
                    item3.transform.parent = __instance.m_OpenCardBoxSpawnCardPackPosList[i];
                    item3.transform.localScale = __instance.m_OpenCardBoxSpawnCardPackPosList[i].localScale;
                    item3.gameObject.SetActive(true);
                    __instance.m_HoldItemList.Add(item3);
                    CPlayerData.m_HoldItemTypeList.Add(item3.GetItemType());
                    __instance.StartCoroutine(__instance.DelayLerpSpawnedCardPackToHand(i, (1.25f / Plugin.SpeedMultiplierValue) + 0.05f * ((float)i / Plugin.SpeedMultiplierValue), item3, __instance.m_HoldCardPackPosList[i], item2));
                }
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InteractionPlayerController), nameof(InteractionPlayerController.RaycastHoldItemState))]
        public static bool InteractionPlayerController_RaycastHoldItemState_Prefix(ref InteractionPlayerController __instance, out float __state)
        {
            __state = __instance.m_MouseHoldAutoFireRate;
            if (!Plugin.EnableMod.Value) return true;

            __instance.m_MouseHoldAutoFireRate /= Plugin.PickupSpeedMultiplierValue;

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InteractionPlayerController), nameof(InteractionPlayerController.RaycastHoldItemState))]
        public static void InteractionPlayerController_RaycastHoldItemState_Postfix(ref InteractionPlayerController __instance, float __state)
        {
            if (!Plugin.EnableMod.Value) return;

            __instance.m_MouseHoldAutoFireRate = __state;

            return;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InteractionPlayerController), nameof(InteractionPlayerController.RaycastNormalState))]
        public static bool InteractionPlayerController_RaycastNormalState_Prefix(ref InteractionPlayerController __instance, out float __state)
        {
            __state = __instance.m_MouseHoldAutoFireRate;
            if (!Plugin.EnableMod.Value) return true;

            __instance.m_MouseHoldAutoFireRate /= Plugin.PickupSpeedMultiplierValue;

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InteractionPlayerController), nameof(InteractionPlayerController.RaycastNormalState))]
        public static void InteractionPlayerController_RaycastNormalState_Postfix(ref InteractionPlayerController __instance, float __state)
        {
            if (!Plugin.EnableMod.Value) return;

            __instance.m_MouseHoldAutoFireRate = __state;

            return;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InteractionPlayerController), nameof(InteractionPlayerController.OnGameDataFinishLoaded))]
        public static bool InteractionPlayerController_OnGameDataFinishLoaded_Prefix(ref InteractionPlayerController __instance)
        {
            if (__instance.m_HoldCardPackPosList.Count < 64)
            {
                List<Transform> transformsList = __instance.m_HoldCardPackPosList;
                Transform baseTransform = __instance.m_HoldCardPackPosList.Last();
                Vector3 basePosition = baseTransform.position;
                Vector3 lastDelta = transformsList[transformsList.Count - 1].position - transformsList[transformsList.Count - 2].position;

                for (int i = 0; i < 54; i++)
                {
                    Vector3 newPosition = transformsList[transformsList.Count - 1].position + lastDelta;
                    newPosition = basePosition + (newPosition - basePosition);
                    GameObject newGameObject = new GameObject("HoldPackPositionLoc (" + (transformsList.Count) + ")");
                    Transform newTransform = newGameObject.transform;
                    newTransform.parent = baseTransform;
                    newTransform.rotation = baseTransform.rotation;
                    newTransform.localScale = baseTransform.localScale;
                    newTransform.position = newPosition;
                    transformsList.Add(newTransform);
                }
            }

            return true;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(InteractionPlayerController), nameof(InteractionPlayerController.EvaluateTakeItemFromShelf))]
        public static IEnumerable<CodeInstruction> InteractionPlayerController_EvaluateTakeItemFromShelf_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (!Plugin.EnableMod.Value || !Plugin.EnableMaxHoldPacksValue) return instructions;

            var code = new List<CodeInstruction>(instructions);

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Ldc_I4_8)
                {
                    code[i] = new CodeInstruction(OpCodes.Call, AccessTools.Property(typeof(Plugin), "MaxHoldPacksValue").GetGetMethod());
                }
            }
            return code;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CardOpeningSequenceUI), nameof(CardOpeningSequenceUI.Update))]
        public static bool CardOpeningSequenceUI_Update_Prefix(ref CardOpeningSequenceUI __instance)
        {
            if (!Plugin.EnableMod.Value) return true;

            if (__instance.m_IsShowingTotalValue)
            {
                __instance.m_TotalValueLerpTimer += (Time.deltaTime * 0.5f) * Plugin.SpeedMultiplierValue;
                __instance.m_CurrentTotalCardValueLerp = Mathf.Lerp(0f, __instance.m_TargetTotalCardValueLerp, __instance.m_TotalValueLerpTimer);
                __instance.m_TotalCardValueText.text = GameInstance.GetPriceString(__instance.m_CurrentTotalCardValueLerp, false, true, false, "F2");
                if (__instance.m_TotalValueLerpTimer >= 1f)
                {
                    __instance.m_IsShowingTotalValue = false;
                    SoundManager.SetEnableSound_ExpIncrease(false);
                }
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CardOpeningSequenceUI), nameof(CardOpeningSequenceUI.ShowSingleCardValue))]
        public static bool CardOpeningSequenceUI_ShowSingleCardValue_Prefix(ref CardOpeningSequenceUI __instance, ref float cardValue)
        {
            if (!Plugin.EnableMod.Value) return true;

            if (!__instance.m_ScreenGrp.activeSelf)
            {
                __instance.m_ScreenGrp.SetActive(true);
            }
            __instance.m_CardValueText.text = GameInstance.GetPriceString(cardValue, false, true, false, "F2");
            __instance.m_CardValueTextGrp.SetActive(true);

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CardOpeningSequence), nameof(CardOpeningSequence.ReadyingCardPack))]
        public static void CardOpeningSequence_ReadyingCardPack_Postfix(ref CardOpeningSequence __instance)
        {
            if (!Plugin.EnableMod.Value) return;

            __instance.m_MultiplierStateTimer = (1f + 2.5f * CSingleton<CGameManager>.Instance.m_OpenPackSpeedSlider) * Plugin.SpeedMultiplierValue;

            return;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CardOpeningSequence), nameof(CardOpeningSequence.Update))]
        public static bool CardOpeningSequence_Update_Prefix(ref CardOpeningSequence __instance)
        {
            if (!Plugin.EnableMod.Value) {
                __instance.m_HighValueCardThreshold = 10f;
                __instance.m_CardOpeningRotateToFrontAnim["CardOpenSeq1_RotateToFront"].speed = 1f;
                __instance.m_CardOpeningRotateToFrontAnim["CardOpenSeq0_Idle"].speed = 1f;

                foreach (var card in __instance.m_CardAnimList)
                {
                    card["OpenCardNewCard"].speed = 1f;
                }
                return true;
            }

            if (Plugin.AutoOpenKey.Value.IsDown())
            {
                ToggleAutoOpen();
            }

            __instance.m_CardOpeningRotateToFrontAnim["CardOpenSeq1_RotateToFront"].speed = Plugin.SpeedMultiplierValue;
            __instance.m_CardOpeningRotateToFrontAnim["CardOpenSeq0_Idle"].speed = Plugin.SpeedMultiplierValue;
            __instance.m_HighValueCardThreshold = Plugin.HighValueThreshold.Value;

            foreach (var card in __instance.m_CardAnimList)
            {
                card["OpenCardNewCard"].speed = Plugin.SpeedMultiplierValue;
            }

            if (PackSpeedMultiplier != (Plugin.SpeedMultiplierValue - __instance.m_MultiplierStateTimer))
            {
                PackSpeedMultiplier = Mathf.Clamp(Plugin.SpeedMultiplierValue - __instance.m_MultiplierStateTimer, 0f, Plugin.SpeedMultiplierValue);
            }

            LogTimer += Time.deltaTime;
            if (LogTimer > 0.5f)
            {
                Plugin.L($"Current State: {__instance.m_StateIndex}");
                LogTimer = 0f;
            }

            __instance.m_IsAutoFire = false;
            if (IsAutoOpen)
            {
                __instance.m_IsAutoFire = true;
            }
            else
            {
                __instance.m_IsAutoFire = false;
            }
            if (!__instance.m_IsScreenActive)
            {
                return false;
            }
            if (InputManager.GetKeyDownAction(EGameAction.OpenPack))
            {
                __instance.m_IsAutoFireKeydown = true;
            }
            if (InputManager.GetKeyUpAction(EGameAction.OpenPack))
            {
                __instance.m_IsAutoFireKeydown = false;
            }
            if (__instance.m_IsAutoFireKeydown)
            {
                __instance.m_AutoFireTimer += Time.deltaTime;
                if (__instance.m_AutoFireTimer >= 0.05f)
                {
                    __instance.m_AutoFireTimer = 0f;
                    __instance.m_IsAutoFire = true;
                }
            }
            else if (__instance.m_AutoFireTimer > 0f)
            {
                __instance.m_AutoFireTimer = 0f;
                __instance.m_IsAutoFire = true;
            }
            if (__instance.m_IsReadyingToOpen)
            {
                if (!__instance.m_IsReadyToOpen)
                {
                    if (__instance.m_IsCanceling)
                    {
                        __instance.m_LerpPosTimer -= Time.deltaTime * __instance.m_LerpPosSpeed;
                        if (__instance.m_LerpPosTimer < 0f)
                        {
                            __instance.m_LerpPosTimer = 0f;
                            __instance.m_IsReadyToOpen = false;
                            __instance.m_IsReadyingToOpen = false;
                            __instance.m_IsCanceling = false;
                            __instance.m_IsScreenActive = false;
                            __instance.m_CardPackAnimator.gameObject.SetActive(false);
                            CSingleton<InteractionPlayerController>.Instance.ExitLockMoveMode();
                            CSingleton<InteractionPlayerController>.Instance.OnExitOpenPackState();
                            InteractionPlayerController.RestoreHiddenToolTip();
                            __instance.m_CurrentItem.gameObject.SetActive(true);
                            InteractionPlayerController.SetAllHoldItemVisibility(true);
                            __instance.m_CurrentItem = null;
                            TutorialManager.SetGameUIVisible(true);
                            CenterDot.SetVisibility(true);
                            GameUIScreen.ResetEnterGoNextDayIndicatorVisible();
                        }
                    }
                    else
                    {
                        __instance.m_LerpPosTimer += (Time.deltaTime * __instance.m_LerpPosSpeed) * Plugin.SpeedMultiplierValue;
                        if (__instance.m_LerpPosTimer > 1f)
                        {
                            __instance.m_LerpPosTimer = 1f;
                            __instance.m_IsReadyToOpen = true;
                        }
                    }
                    __instance.m_CardPackAnimator.transform.localPosition = Vector3.Lerp(__instance.m_StartLerpTransform.localPosition, Vector3.zero, __instance.m_LerpPosTimer);
                    __instance.m_CardPackAnimator.transform.localRotation = Quaternion.Lerp(__instance.m_StartLerpTransform.localRotation, Quaternion.identity, __instance.m_LerpPosTimer);
                    __instance.m_CardPackAnimator.transform.localScale = Vector3.Lerp(__instance.m_StartLerpTransform.localScale, Vector3.one, __instance.m_LerpPosTimer);
                    return false;
                }
                if (__instance.m_IsAutoFire)
                {
                    __instance.m_IsReadyingToOpen = false;
                    __instance.OpenScreen(InventoryBase.ItemTypeToCollectionPackType(__instance.m_CurrentItem.GetItemType()), false, false);
                    return false;
                }
                if (InputManager.GetKeyDownAction(EGameAction.CancelOpenPack) && !__instance.m_IsCanceling)
                {
                    CSingleton<InteractionPlayerController>.Instance.AddHoldItemToFront(__instance.m_CurrentItem);
                    __instance.m_IsCanceling = true;
                    __instance.m_IsReadyToOpen = false;
                    CSingleton<InteractionPlayerController>.Instance.m_BlackBGWorldUIFade.SetFadeOut(3f, 0f);
                    InteractionPlayerController.RestoreHiddenToolTip();
                    CSingleton<InteractionPlayerController>.Instance.m_CameraFOVController.StopLerpFOV();
                    SoundManager.GenericPop(1f, 0.9f);
                }
                return false;
            }
            else
            {
                if (!__instance.m_IsScreenActive)
                {
                    return false;
                }
                if (__instance.m_StateIndex == 0)
                {
                    __instance.InitOpenSequence();
                    __instance.m_StateIndex++;
                    return false;
                }
                if (__instance.m_StateIndex == 1)
                {
                    __instance.m_StateTimer += Time.deltaTime * (__instance.m_MultiplierStateTimer + PackSpeedMultiplier);
                    if (__instance.m_StateTimer > 0.05f)
                    {
                        __instance.m_StateTimer = 0f;
                        if (__instance.m_TempIndex < __instance.m_Card3dUIList.Count)
                        {
                            __instance.m_Card3dUIList[__instance.m_TempIndex].gameObject.SetActive(true);
                            __instance.m_TempIndex++;
                        }
                    }
                    if (__instance.m_IsAutoFire || __instance.m_IsAutoFireKeydown || CSingleton<CGameManager>.Instance.m_OpenPacAutoNextCard)
                    {
                        __instance.m_Slider += (0.0065f * (__instance.m_MultiplierStateTimer + PackSpeedMultiplier));
                        __instance.m_CardPackAnimator.Play("PackOpenAnim", -1, __instance.m_Slider);
                        if (__instance.m_Slider >= 0.3f)
                        {
                            __instance.m_OpenPackVFX.Play();
                            SoundManager.PlayAudio("SFX_OpenPack", 0.6f, 1f);
                            SoundManager.PlayAudio("SFX_BoxOpen", 0.5f, 1f);
                            __instance.m_StateIndex++;
                            return false;
                        }
                    }
                }
                else if (__instance.m_StateIndex == 2)
                {
                    __instance.m_Slider += Time.deltaTime * 1f * (__instance.m_MultiplierStateTimer + PackSpeedMultiplier);
                    __instance.m_CardPackAnimator.Play("PackOpenAnim", -1, __instance.m_Slider);
                    __instance.m_StateTimer += Time.deltaTime * Plugin.SpeedMultiplierValue;
                    if (__instance.m_StateTimer > 0.05f)
                    {
                        __instance.m_StateTimer = 0f;
                        if (__instance.m_TempIndex < __instance.m_Card3dUIList.Count)
                        {
                            __instance.m_Card3dUIList[__instance.m_TempIndex].gameObject.SetActive(true);
                            __instance.m_TempIndex++;
                        }
                    }
                    if (__instance.m_Slider >= 1f)
                    {
                        InteractionPlayerController.RemoveToolTip(EGameAction.OpenPack);
                        __instance.m_TempIndex = 0;
                        __instance.m_StateTimer = 0f;
                        __instance.m_Slider = 0f;
                        __instance.m_StateIndex++;
                        for (int i = 0; i < __instance.m_Card3dUIList.Count; i++)
                        {
                            __instance.m_Card3dUIList[i].gameObject.SetActive(true);
                        }
                        return false;
                    }
                }
                else if (__instance.m_StateIndex == 3)
                {
                    __instance.m_Slider += Time.deltaTime * 1f * (__instance.m_MultiplierStateTimer + PackSpeedMultiplier);
                    if (__instance.m_Slider >= 0.15f)
                    {
                        __instance.m_Slider = 0f;
                        __instance.m_StateIndex++;
                        __instance.m_CardOpeningRotateToFrontAnim.Play("CardOpenSeq1_RotateToFront");
                    }
                    if (__instance.m_IsAutoFire || CSingleton<CGameManager>.Instance.m_OpenPacAutoNextCard)
                    {
                        float num = 0.002f * (float)__instance.m_CurrentOpenedCardIndex;
                        float num2 = 0.001f * (float)__instance.m_CurrentOpenedCardIndex;
                        SoundManager.PlayAudio("SFX_CardReveal1", 0.6f + num2, 1f + num);
                        __instance.m_CardOpeningRotateToFrontAnim.Play("CardOpenSeq1_RotateToFront");
                        __instance.m_StateTimer = 0f;
                        __instance.m_StateIndex++;
                        return false;
                    }
                }
                else if (__instance.m_StateIndex == 4)
                {
                    __instance.m_Slider += Time.deltaTime * 1f * (__instance.m_MultiplierStateTimer + PackSpeedMultiplier);
                    if (!__instance.m_CardOpeningSequenceUI.m_CardValueTextGrp.activeSelf && __instance.m_CurrentOpenedCardIndex < 6 && __instance.m_Slider >= 0.45f && !__instance.m_IsNewlList[__instance.m_CurrentOpenedCardIndex])
                    {
                        __instance.m_TotalCardValue += __instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex];
                        __instance.m_CardOpeningSequenceUI.ShowSingleCardValue(__instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex]);
                    }
                    if (__instance.m_Slider >= 0.8f)
                    {
                        __instance.m_Slider = 0f;
                        if (__instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex] >= __instance.m_HighValueCardThreshold)
                        {
                            if (Plugin.StopAutoHighValue.Value && IsAutoOpen)
                            {
                                IsAutoOpen = false;
                                DelayAutoOpen = true;
                            }
                            SoundManager.PlayAudio("SFX_FinalizeCard", 0.6f, 1.2f);
                            __instance.m_CardAnimList[__instance.m_CurrentOpenedCardIndex].Play("OpenCardNewCard");
                            __instance.m_HighValueCardIcon.SetActive(true);
                            __instance.StartCoroutine(__instance.DelayToState(5, 0.9f));
                            __instance.m_TotalCardValue += __instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex];
                            __instance.m_CardOpeningSequenceUI.ShowSingleCardValue(__instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex]);
                            __instance.m_IsGetHighValueCard = true;
                            return false;
                        }
                        if (__instance.m_IsNewlList[__instance.m_CurrentOpenedCardIndex])
                        {
                            SoundManager.PlayAudio("SFX_CardReveal0", 0.6f, 1f);
                            __instance.m_CardAnimList[__instance.m_CurrentOpenedCardIndex].Play("OpenCardNewCard");
                            __instance.m_NewCardIcon.SetActive(true);
                            __instance.StartCoroutine(__instance.DelayToState(5, 0.9f));
                            __instance.m_TotalCardValue += __instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex];
                            __instance.m_CardOpeningSequenceUI.ShowSingleCardValue(__instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex]);
                            return false;
                        }
                        __instance.m_StateIndex++;
                        return false;
                    }
                }
                else if (__instance.m_StateIndex == 5)
                {
                    if (__instance.m_IsAutoFire || (!__instance.m_IsGetHighValueCard && CSingleton<CGameManager>.Instance.m_OpenPacAutoNextCard))
                    {
                        int num3 = UnityEngine.Random.Range(0, 3);
                        float num4 = 0.002f * (float)__instance.m_CurrentOpenedCardIndex;
                        float num5 = 0.001f * (float)__instance.m_CurrentOpenedCardIndex;
                        if (num3 == 0)
                        {
                            SoundManager.PlayAudio("SFX_CardReveal1", 0.6f + num5, 1f + num4);
                        }
                        else if (num3 == 1)
                        {
                            SoundManager.PlayAudio("SFX_CardReveal2", 0.6f + num5, 1f + num4);
                        }
                        else
                        {
                            SoundManager.PlayAudio("SFX_CardReveal3", 0.6f + num5, 1f + num4);
                        }
                        if (__instance.m_CurrentOpenedCardIndex >= 7)
                        {
                            __instance.m_StateIndex = 7;
                        }
                        else
                        {
                            __instance.m_StateIndex++;
                            __instance.m_NewCardIcon.SetActive(false);
                            __instance.m_HighValueCardIcon.SetActive(false);
                            __instance.m_CardAnimList[__instance.m_CurrentOpenedCardIndex].Play("OpenCardSlideExit");
                            __instance.m_CardAnimList[__instance.m_CurrentOpenedCardIndex]["OpenCardSlideExit"].speed = 1f * (__instance.m_MultiplierStateTimer + PackSpeedMultiplier);
                            __instance.m_CardOpeningSequenceUI.HideSingleCardValue();
                        }
                        __instance.m_IsGetHighValueCard = false;
                        if (DelayAutoOpen && !IsAutoOpen)
                        {
                            IsAutoOpen = true;
                        }
                        DelayAutoOpen = false;
                        return false;
                    }
                }
                else if (__instance.m_StateIndex == 6)
                {
                    __instance.m_Slider += Time.deltaTime * 1f * (__instance.m_MultiplierStateTimer + PackSpeedMultiplier);
                    if (!__instance.m_CardOpeningSequenceUI.m_CardValueTextGrp.activeSelf && __instance.m_CurrentOpenedCardIndex < 6 && __instance.m_Slider >= 0.3f && !__instance.m_IsNewlList[__instance.m_CurrentOpenedCardIndex + 1] && __instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex + 1] < __instance.m_HighValueCardThreshold)
                    {
                        __instance.m_TotalCardValue += __instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex + 1];
                        __instance.m_CardOpeningSequenceUI.ShowSingleCardValue(__instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex + 1]);
                    }
                    if (__instance.m_Slider >= 0.5f)
                    {
                        __instance.m_Slider = 0f;
                        if (__instance.m_Card3dUIList.Count > __instance.m_CurrentOpenedCardIndex)
                        {
                            __instance.m_CardAnimList[__instance.m_CurrentOpenedCardIndex].transform.localPosition = Vector3.zero;
                            __instance.m_Card3dUIList[__instance.m_CurrentOpenedCardIndex].gameObject.SetActive(false);
                        }
                        __instance.m_CurrentOpenedCardIndex++;
                        if (__instance.m_CurrentOpenedCardIndex >= 7)
                        {
                            __instance.m_IsGetHighValueCard = false;
                            __instance.m_StateIndex = 7;
                            return false;
                        }
                        if (__instance.m_Card3dUIList.Count > __instance.m_CurrentOpenedCardIndex + 1)
                        {
                            __instance.m_Card3dUIList[__instance.m_CurrentOpenedCardIndex + 1].gameObject.SetActive(true);
                        }
                        if (__instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex] >= __instance.m_HighValueCardThreshold)
                        {
                            if (Plugin.StopAutoHighValue.Value && IsAutoOpen)
                            {
                                IsAutoOpen = false;
                                DelayAutoOpen = true;
                            }
                            SoundManager.PlayAudio("SFX_FinalizeCard", 0.6f, 1.2f);
                            __instance.m_CardAnimList[__instance.m_CurrentOpenedCardIndex].Play("OpenCardNewCard");
                            __instance.m_HighValueCardIcon.SetActive(true);
                            __instance.StartCoroutine(__instance.DelayToState(5, 0.9f));
                            __instance.m_TotalCardValue += __instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex];
                            __instance.m_CardOpeningSequenceUI.ShowSingleCardValue(__instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex]);
                            __instance.m_IsGetHighValueCard = true;
                            return false;
                        }
                        if (__instance.m_IsNewlList[__instance.m_CurrentOpenedCardIndex])
                        {
                            SoundManager.PlayAudio("SFX_CardReveal0", 0.6f, 1f);
                            __instance.m_CardAnimList[__instance.m_CurrentOpenedCardIndex].Play("OpenCardNewCard");
                            __instance.m_NewCardIcon.SetActive(true);
                            __instance.StartCoroutine(__instance.DelayToState(5, 0.9f));
                            __instance.m_TotalCardValue += __instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex];
                            __instance.m_CardOpeningSequenceUI.ShowSingleCardValue(__instance.m_CardValueList[__instance.m_CurrentOpenedCardIndex]);
                            return false;
                        }
                        __instance.m_StateIndex = 5;
                        return false;
                    }
                }
                else if (__instance.m_StateIndex == 7)
                {
                    if (DelayAutoOpen && !IsAutoOpen)
                    {
                        IsAutoOpen = true;
                    }
                    DelayAutoOpen = false;
                    if (__instance.m_StateTimer == 0f && __instance.m_Slider == 0f)
                    {
                        SoundManager.PlayAudio("SFX_PercStarJingle3", 0.6f, 1f);
                        SoundManager.PlayAudio("SFX_Gift", 0.6f, 1f);
                    }
                    __instance.m_Slider += Time.deltaTime;
                    if (__instance.m_Slider >= 0.05f)
                    {
                        __instance.m_Slider = 0f;
                        __instance.m_CardAnimList[(int)__instance.m_StateTimer].transform.position = __instance.m_ShowAllCardPosList[(int)__instance.m_StateTimer].position;
                        __instance.m_CardAnimList[(int)__instance.m_StateTimer].transform.rotation = __instance.m_ShowAllCardPosList[(int)__instance.m_StateTimer].rotation;
                        __instance.m_Card3dUIList[(int)__instance.m_StateTimer].gameObject.SetActive(true);
                        __instance.m_CardAnimList[(int)__instance.m_StateTimer].Play("OpenCardFinalReveal");
                        __instance.m_StateTimer += 1f;
                        if (__instance.m_StateTimer >= (float)__instance.m_Card3dUIList.Count)
                        {
                            __instance.m_StateTimer = 0f;
                            __instance.m_StateIndex++;
                            __instance.m_CardOpeningSequenceUI.StartShowTotalValue(__instance.m_TotalCardValue, __instance.m_HasFoilCard);
                            return false;
                        }
                    }
                }
                else if (__instance.m_StateIndex == 8)
                {
                    __instance.m_StateTimer += Time.deltaTime;
                    if (__instance.m_StateTimer >= 0.02f)
                    {
                        __instance.m_Slider = 0f;
                        __instance.m_Card3dUIList[(int)__instance.m_StateTimer].m_NewCardIndicator.gameObject.SetActive(__instance.m_RolledCardDataList[(int)__instance.m_StateTimer].isNew);
                        __instance.m_StateTimer += 1f;
                        if (__instance.m_StateTimer >= (float)__instance.m_Card3dUIList.Count)
                        {
                            __instance.m_StateIndex++;
                            return false;
                        }
                    }
                }
                else if (__instance.m_StateIndex == 9)
                {
                    __instance.m_Slider += Time.deltaTime;
                    if (__instance.m_Slider >= 1f / (Plugin.SpeedMultiplierValue / 2))
                    {
                        __instance.m_Slider = 0f;
                        __instance.m_StateIndex++;
                        return false;
                    }
                }
                else if (__instance.m_StateIndex == 10)
                {
                    if (__instance.m_IsAutoFire)
                    {
                        __instance.m_StateIndex++;
                        return false;
                    }
                }
                else if (__instance.m_StateIndex == 11)
                {
                    __instance.m_StateTimer += Time.deltaTime * 1f;
                    if (__instance.m_StateTimer >= 0.01f)
                    {
                        __instance.m_Slider = 0f;
                        __instance.m_IsScreenActive = false;
                        __instance.m_IsReadyToOpen = false;
                        __instance.m_CardPackAnimator.gameObject.SetActive(false);
                        __instance.m_CardOpeningUIGroup.SetActive(false);
                        __instance.m_CardOpeningSequenceUI.HideTotalValue();
                        CSingleton<InteractionPlayerController>.Instance.ExitLockMoveMode();
                        CSingleton<InteractionPlayerController>.Instance.OnExitOpenPackState();
                        __instance.m_CurrentItem.DisableItem();
                        __instance.m_CurrentItem = null;
                        int num6 = 0;
                        __instance.m_TotalCardValue = 0f;
                        __instance.m_TotalExpGained = 0;
                        bool isGet = false;
                        bool isGet2 = false;
                        for (int j = 0; j < __instance.m_RolledCardDataList.Count; j++)
                        {
                            int num7 = (int)((int)(__instance.m_RolledCardDataList[j].GetCardBorderType() + 1) * Mathf.CeilToInt((float)(__instance.m_RolledCardDataList[j].borderType + 1) / 2f));
                            if (__instance.m_RolledCardDataList[j].isFoil)
                            {
                                num7 *= 8;
                            }
                            __instance.m_TotalExpGained += num7;
                            if (__instance.m_RolledCardDataList[j].GetCardBorderType() == ECardBorderType.FullArt && __instance.m_RolledCardDataList[j].isFoil)
                            {
                                isGet = true;
                                if (__instance.m_RolledCardDataList[j].expansionType == ECardExpansionType.Ghost)
                                {
                                    isGet2 = true;
                                }
                            }
                            if (__instance.m_RolledCardDataList[j].isNew)
                            {
                                num6++;
                            }
                        }
                        if (__instance.m_TotalExpGained > 0)
                        {
                            CEventManager.QueueEvent(new CEventPlayer_AddShopExp(__instance.m_TotalExpGained, false));
                        }
                        for (int k = 0; k < __instance.m_CardAnimList.Count; k++)
                        {
                            __instance.m_CardAnimList[k].transform.localPosition = Vector3.zero;
                            __instance.m_CardAnimList[k].transform.localRotation = Quaternion.identity;
                            __instance.m_Card3dUIList[k].m_NewCardIndicator.gameObject.SetActive(false);
                            __instance.m_CardAnimList[k].Play("OpenCardDefaultPos");
                        }
                        if (CSingleton<InteractionPlayerController>.Instance.GetHoldItemCount() <= 0)
                        {
                            TutorialManager.SetGameUIVisible(true);
                            CenterDot.SetVisibility(true);
                            GameUIScreen.ResetEnterGoNextDayIndicatorVisible();
                            CSingleton<InteractionPlayerController>.Instance.m_BlackBGWorldUIFade.SetFadeOut(3f, 0f);
                            CSingleton<InteractionPlayerController>.Instance.m_CameraFOVController.StopLerpFOV();
                            __instance.m_IsAutoFireKeydown = false;
                            __instance.m_AutoFireTimer = 0f;
                        }
                        CSingleton<InteractionPlayerController>.Instance.EvaluateOpenCardPack();
                        TutorialManager.AddTaskValue(ETutorialTaskCondition.OpenPack, 1f);
                        CPlayerData.m_GameReportDataCollect.cardPackOpened = CPlayerData.m_GameReportDataCollect.cardPackOpened + 1;
                        CPlayerData.m_GameReportDataCollectPermanent.cardPackOpened = CPlayerData.m_GameReportDataCollectPermanent.cardPackOpened + 1;
                        AchievementManager.OnCardPackOpened(CPlayerData.m_GameReportDataCollectPermanent.cardPackOpened);
                        AchievementManager.OnGetFullArtFoil(isGet);
                        AchievementManager.OnGetFullArtGhostFoil(isGet2);
                        if (num6 > 0)
                        {
                            AchievementManager.OnCheckAlbumCardCount(CPlayerData.GetTotalCardCollectedAmount());
                        }
                        UnityAnalytic.OpenPack();
                        return false;
                    }
                }
                else
                {
                    if (__instance.m_StateIndex == 12)
                    {
                        __instance.m_IsScreenActive = false;
                        return false;
                    }
                    if (__instance.m_StateIndex == 101)
                    {
                        float stateTimer = __instance.m_StateTimer;
                        __instance.m_StateTimer += Time.deltaTime;
                        if (__instance.m_StateTimer >= 0.05f)
                        {
                            int num8 = UnityEngine.Random.Range(0, 3);
                            float num9 = 0.002f * (float)__instance.m_CurrentOpenedCardIndex;
                            float num10 = 0.001f * (float)__instance.m_CurrentOpenedCardIndex;
                            if (num8 == 0)
                            {
                                SoundManager.PlayAudio("SFX_CardReveal1", 0.6f + num10, 1f + num9);
                            }
                            else if (num8 == 1)
                            {
                                SoundManager.PlayAudio("SFX_CardReveal2", 0.6f + num10, 1f + num9);
                            }
                            else
                            {
                                SoundManager.PlayAudio("SFX_CardReveal3", 0.6f + num10, 1f + num9);
                            }
                            __instance.m_CurrentOpenedCardIndex++;
                            return false;
                        }
                    }
                    else
                    {
                        int stateIndex = __instance.m_StateIndex;
                    }
                }
                return false;
            }
        }
        public static void ToggleAutoOpen()
        {
            if (IsAutoOpen)
            {
                IsAutoOpen = false;
                return;
            }
            IsAutoOpen = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameUIScreen), nameof(GameUIScreen.Update))]
        public static void GameUIScreen_Update_Postfix(ref GameUIScreen __instance)
        {
            if (holdItemCountText == null && __instance.m_DayText != null)
            {
                DuplicateUIComponents(__instance);
                holdItemCountText.gameObject.SetActive(true);
            }
            if (InteractionPlayerController.instance.m_HoldItemList.Count <= 0)
            {
                holdItemCountText.text = "";
            }
            else if (InteractionPlayerController.instance.m_HoldItemList[0].m_ItemType.ToString().Contains("CardPack"))
            {
                holdItemCountText.text = "Held packs: " + InteractionPlayerController.instance.m_HoldItemList.Count.ToString();
            }
            return;
        }

        public static TextMeshProUGUI holdItemCountText;
        public static RectTransform holdItemCountRectTransform;
        public static GameObject newGameObject;

        public static void DuplicateUIComponents(GameUIScreen gameUIScreen)
        {
            newGameObject = new GameObject("HoldItemCountUI");
            Transform newTransform = gameUIScreen.m_DayText.transform.GetComponentInParent<RectTransform>().transform.parent;
            newGameObject.transform.SetParent(newTransform, false);

            holdItemCountRectTransform = newGameObject.AddComponent<RectTransform>();
            RectTransform originalRectTransform = gameUIScreen.m_DayText.transform.GetComponentInParent<RectTransform>();

            holdItemCountRectTransform.anchorMin = originalRectTransform.anchorMin;
            holdItemCountRectTransform.anchorMax = originalRectTransform.anchorMax;
            holdItemCountRectTransform.pivot = originalRectTransform.pivot;
            holdItemCountRectTransform.anchoredPosition = originalRectTransform.anchoredPosition;
            holdItemCountRectTransform.sizeDelta = originalRectTransform.sizeDelta;

            holdItemCountRectTransform.anchorMin = new Vector2(0, 0);
            holdItemCountRectTransform.anchorMax = new Vector2(1, 1);
            holdItemCountRectTransform.pivot = new Vector2(0, 0);
            holdItemCountRectTransform.anchoredPosition = Vector2.zero;
            holdItemCountRectTransform.sizeDelta = new Vector2(200, 100);

            holdItemCountText = Object.Instantiate(gameUIScreen.m_DayText, holdItemCountRectTransform);
            holdItemCountText.text = "";
            holdItemCountText.fontSizeMax = Plugin.TextSize.Value;
            holdItemCountText.fontSize = Plugin.TextSize.Value;
            holdItemCountText.fontSizeMin = 1f;
            holdItemCountText.autoSizeTextContainer = false;
            holdItemCountText.alignment = TextAlignmentOptions.Left;
            holdItemCountText.transform.localPosition = new Vector3(Plugin.TextPositionX.Value, Plugin.TextPositionY.Value, 0);
        }
    }
}