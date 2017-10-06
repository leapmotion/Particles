﻿using Leap.Unity.Query;
using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Animation {

  [CustomPropertyDrawer(typeof(SwitchTree), true)]
  public class SwitchTreeDrawer : PropertyDrawer {

    #region Pair Class

    public class Pair<T, U> {
      public T first;
      public U second;

      public Pair(T first, U second) {
        this.first = first;
        this.second = second;
      }
    }

    #endregion

    #region GUI Properties & Colors

    private const float EXTRA_HEIGHT = 6f;
    private const float EXTRA_HEIGHT_PER_NODE = 1f;
    private const float INDENT_WIDTH = 17f;

    private const float BUTTON_RECT_INNER_PAD = 3f;

    private const float LINE_SIDE_MARGIN_RATIO = 0.46f;
    private const float LINE_ORIGIN_RATIO = 0.50f;

    private const float GLOW_WIDTH = 1f;
    private const float GLOW_LINE_SIDE_MARGIN_RATIO = 0.44f;
    private const float GLOW_LINE_ORIGIN_RATIO = 0.48f;


    private static Color backgroundColor {
      get {
        return EditorGUIUtility.isProSkin
            ? new Color32(56, 56, 56, 255)
            : new Color32(194, 194, 194, 255);
      }
    }

    private static Color headerBackgroundColor {
      get { return Color.Lerp(backgroundColor, Color.white, 0.4f); }
    }

    private static Color innerBackgroundColor {
      get { return Color.Lerp(backgroundColor, Color.black, 0.15f); }
    }

    private static Color glowBackgroundColor {
      get { return Color.Lerp(Color.cyan, Color.blue, 0.05f); }
    }

    private static Color glowContentColor {
      get { return Color.Lerp(Color.cyan, Color.white, 0.7f); }
    }

    #endregion

    public override float GetPropertyHeight(SerializedProperty property,
                                            GUIContent label) {
      return (EditorGUIUtility.singleLineHeight + EXTRA_HEIGHT_PER_NODE)
             * makeSwitchTree(property).NodeCount
             + EXTRA_HEIGHT;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

      var switchTree = makeSwitchTree(property);
      int nodeCount = switchTree.NodeCount;

      var visitedNodesCache = Pool<HashSet<SwitchTree.Node>>.Spawn();
      try {
        bool isEvenRow = true;
        foreach (var nodeRectPair in switchTree.Traverse(visitedNodesCache).Query()
                                               .Zip(position.TakeAllLines(nodeCount)
                                                            .Query(), (node, rect) => {
                                                 return new Pair<SwitchTree.Node, Rect>
                                                              (node, rect);
                                                })) {
          drawNode(nodeRectPair.first, nodeRectPair.second, switchTree, isEvenRow);
          isEvenRow = !isEvenRow;
        }
      }
      finally {
        visitedNodesCache.Clear();
        Pool<HashSet<SwitchTree.Node>>.Recycle(visitedNodesCache);
      }

    }

    private SwitchTree makeSwitchTree(SerializedProperty treeProperty) {
      return new SwitchTree((treeProperty.FindPropertyRelative("rootSwitchBehaviour")
                                         .objectReferenceValue as MonoBehaviour).transform);
    }

    private void drawNode(SwitchTree.Node node, Rect rect, SwitchTree switchTree,
                          bool isEvenRow = true) {

      if (node.treeDepth == 0) {
        drawControllerBackground(rect);
      }
      else {
        drawTreeBackground(rect, isEvenRow);
      }
      
      Rect indentRect;
      Rect labelRect = rect.PadLeft(INDENT_WIDTH * (node.treeDepth + 1), out indentRect);
      EditorGUI.LabelField(labelRect,
                           new GUIContent(node.transform.name
                                          + (node.treeDepth == 0 ? " (root)" : "")));

      Rect fullButtonRect = indentRect.TakeRight(INDENT_WIDTH);


      #region Debug Rects

      //EditorGUI.DrawRect(indentRect, Color.cyan);
      //EditorGUI.DrawRect(fullButtonRect, Color.magenta);

      #endregion

      bool isNodeOn = node.isOn;
      bool isParentOn = node.isParentOn;
      bool glowFromParent = isParentOn && node.isOn;

      if (node.treeDepth != 0) {
        if (glowFromParent) {
          drawCenteredLine(Direction4.Left, fullButtonRect, LineType.OuterGlow);
        }

        drawCenteredLine(Direction4.Left, fullButtonRect, (glowFromParent ? LineType.InnerGlow : LineType.Default));
      }
      if (node.hasParent) {
        // Leftward Rect lines

        Rect leftwardRect = indentRect.TakeRight(INDENT_WIDTH * 2f)
                                             .TakeLeft(INDENT_WIDTH);
        
        if (glowFromParent) {
          drawCenteredLine(Direction4.Right | Direction4.Up, leftwardRect, LineType.OuterGlow);
        }

        drawCenteredLine(Direction4.Right | Direction4.Up, leftwardRect,
                         (glowFromParent ? LineType.InnerGlow : LineType.Default));

        if (node.hasPrevSibling) {
          bool prevSiblingGlowFromParent = isParentOn && node.prevSibling.isOn;

          if (prevSiblingGlowFromParent) {
            drawCenteredLine(Direction4.Down | Direction4.Up, leftwardRect, LineType.OuterGlow);
          }

          drawCenteredLine(Direction4.Down, leftwardRect,
                           (prevSiblingGlowFromParent ? LineType.InnerGlow : LineType.Default));

          if (prevSiblingGlowFromParent) {
            drawCenteredLine(Direction4.Up, leftwardRect, LineType.InnerGlow);
          }
        }
      }
      if (node.hasChildren) {
        bool anyChildOn = node.children.Query().Any(n => n.isOn);
        if (anyChildOn) {
          drawCenteredLine(Direction4.Down, fullButtonRect, LineType.OuterGlow);
        }

        drawCenteredLine(Direction4.Down, fullButtonRect, (anyChildOn ? LineType.InnerGlow : LineType.Default));
      }


      Rect buttonRect = fullButtonRect.PadInner(BUTTON_RECT_INNER_PAD);

      // Support undo history.
      Undo.IncrementCurrentGroup();
      var curGroupIdx = Undo.GetCurrentGroup();

      bool test_isNodeOff = node.isOff;
      if (test_isNodeOff) {
        EditorGUI.DrawRect(buttonRect.PadTopPercent(0.50f), Color.red);
      }

      Color origContentColor = GUI.contentColor;
      if (isNodeOn) {
        Rect glowRect = buttonRect.PadOuter(GLOW_WIDTH);
        EditorGUI.DrawRect(glowRect, glowBackgroundColor);
        GUI.contentColor = glowContentColor;
      }

      if (GUI.Button(buttonRect, new GUIContent("Switch to this node."))) {

        // Note: It is the responsibility of the IPropertySwitch implementation
        // to perform operations that correctly report their actions in OnNow() to the
        // Undo history!
        switchTree.SwitchTo(node.transform.name, immediately: !Application.isPlaying);
      }

      Undo.CollapseUndoOperations(curGroupIdx);
      Undo.SetCurrentGroupName("Set Switch Tree State");

      if (isNodeOn) {
        GUI.contentColor = origContentColor;
      }

    }

    private void drawControllerBackground(Rect rect) {
      EditorGUI.DrawRect(rect, headerBackgroundColor);
    }

    private void drawTreeBackground(Rect rect, bool isEvenRow = true) {
      Color color = (isEvenRow ? Color.Lerp(innerBackgroundColor, Color.white, 0.1f) : innerBackgroundColor);
      EditorGUI.DrawRect(rect, color);
    }

    [System.Flags]
    private enum Direction4 {
      Up    = 1 << 0,
      Down  = 1 << 1,
      Left  = 1 << 2,
      Right = 1 << 3
    }

    private enum LineType {
      Default,
      InnerGlow,
      OuterGlow
    }

    private void drawCenteredLine(Direction4 directions, Rect inRect, LineType lineType = LineType.Default) {
      float sideRatio, originRatio;
      switch (lineType) {
        case LineType.OuterGlow:
          sideRatio = GLOW_LINE_SIDE_MARGIN_RATIO;
          originRatio = GLOW_LINE_ORIGIN_RATIO;
          break;
        default:
          sideRatio = LINE_SIDE_MARGIN_RATIO;
          originRatio = LINE_ORIGIN_RATIO;
          break;
      }

      Color lineColor;
      switch (lineType) {
        case LineType.OuterGlow:
          lineColor = glowBackgroundColor;
          break;
        case LineType.InnerGlow:
          lineColor = glowContentColor;
          break;
        default:
          lineColor = Color.black;
          break;
      }

      if ((directions & Direction4.Up) > 0) {
        drawCenteredLineUp(inRect, sideRatio, originRatio, lineColor);
      }
      if ((directions & Direction4.Down) > 0) {
        drawCenteredLineDown(inRect, sideRatio, originRatio, lineColor);
      }
      if ((directions & Direction4.Left) > 0) {
        drawCenteredLineLeft(inRect, sideRatio, originRatio, lineColor);
      }
      if ((directions & Direction4.Right) > 0) {
        drawCenteredLineRight(inRect, sideRatio, originRatio, lineColor);
      }
    }

    private void drawCenteredLineUp(Rect rect, float sideRatio, float originRatio, Color color) {
      Rect middle = rect.PadLeftRightPercent(sideRatio);
      Rect line = middle.PadBottomPercent(originRatio);
      EditorGUI.DrawRect(line, color);
    }

    private void drawCenteredLineDown(Rect rect, float sideRatio, float originRatio, Color color) {
      Rect middle = rect.PadLeftRightPercent(sideRatio);
      Rect line = middle.PadTopPercent(originRatio);
      EditorGUI.DrawRect(line, color);
    }

    private void drawCenteredLineLeft(Rect rect, float sideRatio, float originRatio, Color color) {
      Rect middle = rect.PadTopBottomPercent(sideRatio);
      Rect line = middle.PadRightPercent(originRatio);
      EditorGUI.DrawRect(line, color);
    }

    private void drawCenteredLineRight(Rect rect, float sideRatio, float originRatio, Color color) {
      Rect middle = rect.PadTopBottomPercent(sideRatio);
      Rect line = middle.PadLeftPercent(originRatio);
      EditorGUI.DrawRect(line, color);
    }

    #region GUIStyle nonsense

    private static GUIStyle s_TintableStyle;

    private static GUIStyle TintableStyle {
      get {
        if (s_TintableStyle == null) {
          s_TintableStyle = new GUIStyle();
          s_TintableStyle.normal.background = EditorGUIUtility.whiteTexture;
          s_TintableStyle.stretchWidth = true;
        }
        return s_TintableStyle;
      }
    }

    //private static void DrawEmpty(Rect rect, Color color) {
    //  // Only need to perform drawing during repaints!
    //  if (Event.current.type == EventType.Repaint) {
    //    var restoreColor = GUI.color;
    //    GUI.color = color;
    //    TintableStyle.Draw(rect, false, false, false, false);
    //    GUI.color = restoreColor;
    //  }
    //}


    #endregion

  }

}