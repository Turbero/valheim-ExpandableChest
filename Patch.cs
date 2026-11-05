using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ExpandableChests
{
    [HarmonyPatch(typeof(InventoryGui), "Show")]
    public class InventoryGui_Container_Patch
    {
        public static GameObject containerIncreaseBtnGO;
        public static GameObject containerResetBtnGO;
        
        public static void Postfix(InventoryGui __instance, Container container)
        {
            if (!__instance.IsContainerOpen()) return;

            Piece containerPiece = getContainerPiece(container);
            bool containerIsScope = Enum.TryParse(containerPiece.name.Replace("(Clone)", ""), out ContainerType _);
            if (containerIncreaseBtnGO == null)
            {
                containerIncreaseBtnGO = GameObject.Instantiate(
                    __instance.transform.Find("root/Player/Container/TakeAll").gameObject,
                    __instance.transform.Find("root/Player/Container").transform);
                containerIncreaseBtnGO.name = "NextExpand";
                containerIncreaseBtnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-128, 152);
                containerIncreaseBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
                containerIncreaseBtnGO.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = ConfigurationFile.translateAddRow.Value;
                Button btnIncrease = containerIncreaseBtnGO.GetComponent<Button>();
                btnIncrease.onClick = new Button.ButtonClickedEvent();
                btnIncrease.onClick.AddListener(() =>
                {
                    Container currentContainer = (Container)GameManager.GetPrivateValue(__instance, "m_currentContainer");
                    if (canIncreaseContainer(__instance.m_playerGrid.GetInventory(), currentContainer))
                    {
                        chargeCostToInventory(__instance, currentContainer);
                        updateContainerCost(currentContainer);
                        updateContainerSize(currentContainer);
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, ConfigurationFile.translateContainerCapacityIncreased.Value);
                    }
                });
            }
            containerIncreaseBtnGO.SetActive(ConfigurationFile.allowAddRow.Value && containerIsScope && ConfigurationFile.expandCost.Value >= 0);

            if (containerResetBtnGO == null)
            {
                containerResetBtnGO = GameObject.Instantiate(
                    __instance.transform.Find("root/Player/Container/TakeAll").gameObject,
                    __instance.transform.Find("root/Player/Container").transform);
                containerResetBtnGO.name = "ResetExpands";
                containerResetBtnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(128, 152);
                containerResetBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
                Button btnDecrease = containerResetBtnGO.GetComponent<Button>();
                containerResetBtnGO.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = ConfigurationFile.translateReset.Value;
                btnDecrease.onClick = new Button.ButtonClickedEvent();
                btnDecrease.onClick.AddListener(() =>
                {
                    Container currentContainer = (Container)GameManager.GetPrivateValue(__instance, "m_currentContainer");
                    if (canResetExpandsInContainer(currentContainer))
                    {
                        dropContainerContent(currentContainer);
                        resetContainerSize(currentContainer);
                        resetContainerCost(currentContainer);
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, ConfigurationFile.translateContainerCapacityReset.Value);
                    }
                    else
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, ConfigurationFile.translateContainerCapacityAtMinimumCapacity.Value);
                });
            }
            containerResetBtnGO.SetActive(ConfigurationFile.allowReset.Value && containerIsScope && ConfigurationFile.expandCost.Value >= 0);
        }

        private static bool canIncreaseContainer(Inventory inventory, Container container)
        {
            Piece containerPiece = getContainerPiece(container);
            if (Enum.TryParse(containerPiece.name.Replace("(Clone)", ""), out ContainerType containerType))
            {
                int minHeight = getMinHeight(container);
                int maxExpandedRows = ConfigurationFile.maxExpandedRows.Value;
                if (container.GetInventory().GetHeight() - minHeight >= maxExpandedRows)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, ConfigurationFile.translateMaxExpansionsReached.Value);
                    return false;
                }
                
                if (Player.m_localPlayer.NoCostCheat()) return true;
                
                string originalCost = containerType.GetOriginalCost();
                if (originalCost != null)
                {
                    Piece.Requirement[] reqs = containerPiece.m_resources;
                    string[] mats = originalCost.Split(',');
                    for (int i = 0; i < mats.Length; i++)
                    {
                        string itemName = reqs[i].m_resItem.m_itemData.m_shared.m_name;
                        int originalAmount = int.Parse(mats[i].Split(':')[1]);
                        int neededAmount = originalAmount * ConfigurationFile.expandCost.Value / 100;
                        if (inventory.CountItemsByName(new[] { itemName }) < neededAmount)
                        {
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, ConfigurationFile.translateNotEnoughMaterials.Value);
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static void chargeCostToInventory(InventoryGui __instance, Container container)
        {
            if (Player.m_localPlayer.NoCostCheat()) return;
            
            Piece containerPiece = getContainerPiece(container);
            if (Enum.TryParse(containerPiece.name.Replace("(Clone)", ""), out ContainerType containerType))
            {
                string originalCost = containerType.GetOriginalCost();
                Piece.Requirement[] reqs = containerPiece.m_resources;
                string[] mats = originalCost.Split(',');
                for(int i=0;i<mats.Length;i++)
                {
                    int originalAmount = int.Parse(mats[i].Split(':')[1]);
                    
                    ItemDrop itemDrop = reqs[i].m_resItem;
                    string itemName = itemDrop.m_itemData.m_shared.m_name;
                    int neededAmount = originalAmount * ConfigurationFile.expandCost.Value / 100;
                    __instance.m_playerGrid.GetInventory().RemoveItem(itemName, neededAmount); //Charge cost to inventory
                }

                Logger.Log("Materials removed from inventory");
            }
        }

        private static void updateContainerCost(Container container)
        {
            Piece containerPiece = getContainerPiece(container);
            if (Enum.TryParse(containerPiece.name.Replace("(Clone)", ""), out ContainerType containerType))
            {
                string originalCost = containerType.GetOriginalCost();
                Piece.Requirement[] reqs = containerPiece.m_resources;
                string[] mats = originalCost.Split(',');
                for (int i = 0; i < mats.Length; i++)
                {
                    int originalAmount = int.Parse(mats[i].Split(':')[1]);
                    ItemDrop itemDrop = reqs[i].m_resItem;
                    int neededAmount = (int)(originalAmount * ConfigurationFile.expandCost.Value / 100f);
                    reqs[i].m_amount += neededAmount;
                    Logger.Log($"Added {neededAmount}x {itemDrop.m_itemData.m_shared.m_name} to the piece cost");
                }
            }
        }

        private static void updateContainerSize(Container container)
        {
            int height = container.GetInventory().GetHeight();
            container.GetInventory().SetHeight(height + 1);
        }

        private static bool canResetExpandsInContainer(Container container)
        {
            int minHeight = getMinHeight(container);
            return minHeight < container.GetInventory().GetHeight();
        }

        private static void dropContainerContent(Container container)
        {
            Inventory inventory = container.GetInventory();
            List<ItemDrop.ItemData> m_inventory = (List<ItemDrop.ItemData>)GameManager.GetPrivateValue(inventory, "m_inventory");
            
            for (int i = m_inventory.Count - 1; i >= 0; --i)
            {
                ItemDrop.ItemData itemData = m_inventory[i];
                Player.m_localPlayer.DropItem(inventory, itemData, itemData.m_stack);
            }
            Logger.Log("dropContainerContent done.");
        }
        
        private static void resetContainerSize(Container container)
        {
            container.GetInventory().SetHeight(getMinHeight(container));
            Logger.Log("resetContainerSize done.");
        }
        
        private static void resetContainerCost(Container container)
        {
            Piece containerPiece = getContainerPiece(container);
            if (Enum.TryParse(containerPiece.name.Replace("(Clone)", ""), out ContainerType containerType))
            {
                string originalCost = containerType.GetOriginalCost();
                if (originalCost != null)
                {
                    Piece.Requirement[] reqs = containerPiece.m_resources;
                    string[] mats = originalCost.Split(',');
                    for(int i=0;i<mats.Length;i++)
                    {
                        string prefabId = mats[i].Split(':')[0];
                        int originalAmount = int.Parse(mats[i].Split(':')[1]);
                        Logger.Log("originalAmount: "+originalAmount);
                        
                        int currentAmount = reqs[i].m_amount;
                        Logger.Log("currentAmount: "+currentAmount);
                        //Drop this amount
                        GameObject itemToDrop = ZNetScene.instance.GetPrefab(prefabId);
                        if (itemToDrop == null)
                        {
                            Logger.LogError($"Prefab '{prefabId}' not found.");
                            continue;
                        }
                        GameObject spawnedItem = Object.Instantiate(itemToDrop, Player.m_localPlayer.transform.position, Quaternion.identity);
                        ItemDrop itemDrop = spawnedItem.GetComponent<ItemDrop>();
                        if (itemDrop != null)
                        {
                            Logger.Log("before itemDrop.m_itemData.m_stack: "+itemDrop.m_itemData.m_stack);
                            itemDrop.m_itemData.m_stack = currentAmount - originalAmount;
                            Logger.Log("after itemDrop.m_itemData.m_stack: "+itemDrop.m_itemData.m_stack);
                            Logger.Log($"Item {prefabId} x{itemDrop.m_itemData.m_stack} dropped.");
                        }
                        else
                        {
                            Logger.LogError($"Prefab '{prefabId}' doesn't have an ItemDrop.");
                        }
                        
                        reqs[i].m_amount = originalAmount;
                    }
                }
                Logger.Log("resetContainerCost done.");
            }
        }

        private static int getMinHeight(Container container)
        {
            Piece containerPiece = getContainerPiece(container);
            if (Enum.TryParse(containerPiece.name.Replace("(Clone)", ""), out ContainerType containerType))
            {
                return containerType.GetDefaultRows();
            }
            return int.MaxValue;
        }

        private static Piece getContainerPiece(Container container)
        {
            return (Piece)GameManager.GetPrivateValue(container, "m_piece");
        }
    }
}