using Leap.Unity.Query;
using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Leap.Unity.Animation {

  [CustomPropertyDrawer(typeof(SwitchTree), true)]
  public class SwitchTreeDrawer : PropertyDrawer {

    public class Pair<T, U> {
      public T first;
      public U second;

      public Pair(T first, U second) {
        this.first = first;
        this.second = second;
      }
    }

    #region GUI Properties & Colors

    private static float EXTRA_HEIGHT = 6f;
    private static float INDENT_WIDTH = 15f;

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

    #endregion

    public override float GetPropertyHeight(SerializedProperty property,
                                            GUIContent label) {
      return EditorGUIUtility.singleLineHeight * makeSwitchTree(property).NodeCount
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
          drawNode(nodeRectPair.first, nodeRectPair.second, isEvenRow);
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

    private void drawNode(SwitchTree.Node node, Rect rect, bool isEvenRow = true) {

      if (node.treeDepth == 0) {
        drawControllerBackground(rect);
      }
      else {
        drawTreeBackground(rect);
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
      

      if (node.treeDepth != 0) {
        drawCenteredLine(Direction4.Left, fullButtonRect);
      }
      if (node.hasParent) {
        Rect leftwardRect = indentRect.TakeRight(INDENT_WIDTH * 2f)
                                             .TakeLeft(INDENT_WIDTH);
        drawCenteredLine(Direction4.Right | Direction4.Up, leftwardRect);

        if (node.hasPrevSibling) {
          drawCenteredLine(Direction4.Down, leftwardRect);
        }
      }
      if (node.hasChildren) {
        drawCenteredLine(Direction4.Down, fullButtonRect);
      }


      if (EditorGUI.Toggle(fullButtonRect, false)) {

      }

    }

    private void drawControllerBackground(Rect rect) {
      EditorGUI.DrawRect(rect, headerBackgroundColor);
    }

    private void drawTreeBackground(Rect rect) {
      EditorGUI.DrawRect(rect, innerBackgroundColor);
    }

    [System.Flags]
    private enum Direction4 {
      Up    = 1 << 0,
      Down  = 1 << 1,
      Left  = 1 << 2,
      Right = 1 << 3
    }

    private void drawCenteredLine(Direction4 directions, Rect inRect) {
      if ((directions & Direction4.Up) > 0) {
        drawCenteredLineUp(inRect);
      }
      if ((directions & Direction4.Down) > 0) {
        drawCenteredLineDown(inRect);
      }
      if ((directions & Direction4.Left) > 0) {
        drawCenteredLineLeft(inRect);
      }
      if ((directions & Direction4.Right) > 0) {
        drawCenteredLineRight(inRect);
      }
    }

    private const float LINE_SIDE_MARGIN_RATIO = 0.46f;
    private const float LINE_ORIGIN_RATIO = 0.50f;

    private void drawCenteredLineUp(Rect rect) {
      Rect middle = rect.PadLeftRightPercent(LINE_SIDE_MARGIN_RATIO);
      Rect line = middle.PadBottomPercent(LINE_ORIGIN_RATIO);
      EditorGUI.DrawRect(line, Color.black);
    }

    private void drawCenteredLineDown(Rect rect) {
      Rect middle = rect.PadLeftRightPercent(LINE_SIDE_MARGIN_RATIO);
      Rect line = middle.PadTopPercent(LINE_ORIGIN_RATIO);
      EditorGUI.DrawRect(line, Color.black);
    }

    private void drawCenteredLineLeft(Rect rect) {
      Rect middle = rect.PadTopBottomPercent(LINE_SIDE_MARGIN_RATIO);
      Rect line = middle.PadRightPercent(LINE_ORIGIN_RATIO);
      EditorGUI.DrawRect(line, Color.black);
    }

    private void drawCenteredLineRight(Rect rect) {
      Rect middle = rect.PadTopBottomPercent(LINE_SIDE_MARGIN_RATIO);
      Rect line = middle.PadLeftPercent(LINE_ORIGIN_RATIO);
      EditorGUI.DrawRect(line, Color.black);
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