using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP;
using UnityEngine;

namespace ExLP {

	// Take from TriggerAu's plugin framework

    public class DropDownList
    {
        //properties to use
        internal List<String> Items { get; set; }
        internal Int32 SelectedIndex { get; private set; }
        internal String SelectedValue { get { return Items[SelectedIndex]; } }

        internal Boolean ListVisible;

        private Rect rectButton;
        private Rect rectListBox;

        internal GUIStyle styleListItem;
        internal GUIStyle styleListBox;
        internal GUIStyle styleListBlocker = new GUIStyle();
        internal Int32 ListItemHeight = 20;

        //event for changes
        public delegate void SelectionChangedEventHandler(Int32 OldIndex, Int32 NewIndex);
        public event SelectionChangedEventHandler SelectionChanged;
        
        //Constructors
        public DropDownList(List<String> Items)
            : this()
        {
            this.Items = Items;
        }
        public DropDownList()
        {
            ListVisible = false;
            SelectedIndex = 0;
        }

        //Draw the button behind everything else to catch the first mouse click
        internal void DrawBlockingSelector()
        {
            //do we need to draw the blocker
            if (ListVisible)
            {
                //This will collect the click event before any other controls under the listrect
                if (GUI.Button(rectListBox, "", styleListBlocker))
                {
                    Int32 oldIndex = SelectedIndex;
                    SelectedIndex = (Int32)Math.Floor((Event.current.mousePosition.y - rectListBox.y) / (rectListBox.height / Items.Count));
                    //Throw an event or some such from here
                    SelectionChanged(oldIndex, SelectedIndex);
                    ListVisible = false;
                }

            }
        }

        //Draw the actual button for the list
        internal Boolean DrawButton()
        {
            Boolean blnReturn = false;
            //this is the dropdown button - toggle list visible if clicked
            if (GUILayout.Button(SelectedValue))
            {
                ListVisible = !ListVisible;
                blnReturn = true;
            }
            //get the drawn button rectangle
            if (Event.current.type == EventType.repaint)
                rectButton = GUILayoutUtility.GetLastRect();
            //draw a dropdown symbol on the right edge
            Rect rectDropIcon = new Rect(rectButton) { x = (rectButton.x + rectButton.width - 20), width = 20 };
            GUI.Box(rectDropIcon, "\\/");

            return blnReturn;
        }

        //Draw the hovering dropdown
        internal void DrawDropDown()
        {
            if (ListVisible)
            {
                //work out the list of items box
                rectListBox = new Rect(rectButton)
                {
                    y = rectButton.y + rectButton.height,
                    height = Items.Count * ListItemHeight
                };
                //and draw it
                GUI.Box(rectListBox, "", styleListBox);

                //now draw each listitem
                for (int i = 0; i < Items.Count; i++)
                {
                    Rect ListButtonRect = new Rect(rectListBox) { y = rectListBox.y + (i * ListItemHeight), height = 20 };

                    if (GUI.Button(ListButtonRect, Items[i], styleListItem))
                    {
                        ListVisible = false;
                        SelectedIndex = i;
                    }
                }

                //maybe put this here to limit what happens in pre/post calls
                //CloseOnOutsideClick();
            }

        }

        internal Boolean CloseOnOutsideClick()
        {
            if (ListVisible && Event.current.type == EventType.mouseDown && !rectListBox.Contains(Event.current.mousePosition))
            {
                ListVisible = false;
                return true;
            }
            else { return false; }
        }
        //internal List<GUIContent> List {get;set;}

        //internal void Add(GUIContent NewItem) { List.Add(NewItem); }
        //internal void Add(String NewItem) { List.Add(new GUIContent(NewItem)); }
        //internal void Add(IEnumerable<String> NewItems) { foreach (String NewItem in NewItems) { List.Add(new GUIContent(NewItem)); } }

        //internal void Remove(GUIContent ExistingItem) { if(List.Contains(ExistingItem)) List.Remove(ExistingItem); }


    }
}
