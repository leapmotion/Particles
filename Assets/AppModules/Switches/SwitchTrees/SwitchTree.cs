using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  [Serializable]
  public class SwitchTreeNodeRef {
    public SwitchTreeNode node;

    public SwitchTreeNodeRef(SwitchTreeNode node) {
      this.node = node;
    }

    public SwitchTreeNodeRef() { }
  }

  public static class SwitchTreeNodeRefExtensions {
    public static SwitchTreeNodeRef Ref(this SwitchTreeNode node) {
      return new SwitchTreeNodeRef(node);
    }
  }

  [Serializable]
  public struct SwitchTreeNode {
    public Transform       transform;
    public IPropertySwitch objSwitch;

    public SwitchTreeNodeRef     parent;
    public Stack<SwitchTreeNode> children;

    public int numChildren { get { return children.Count; } }

    public SwitchTreeNode(Transform transform,
                          IPropertySwitch objSwitch,
                          SwitchTreeNodeRef parent = null) {
      this.transform = transform;
      this.objSwitch = objSwitch;

      this.parent = parent;

      children = null;
      constructChildren();
    }

    private void constructChildren() {
      var stack = Pool<Stack<Transform>>.Spawn();
      stack.Clear();
      stack.Push(this.transform);
      try {
        while (stack.Count > 0) {
          var transform = stack.Pop();

          foreach (var child in transform.GetChildren()) {
            // Ignore SwitchTreeControllers, which will handle their own internal
            // hierarchy.
            if (child.GetComponent<SwitchTreeController>() != null) continue;

            // ObjectSwitches get priority, but any Switch will do.
            IPropertySwitch objSwitch = child.GetComponent<ObjectSwitch>();
            if (objSwitch == null) {
              objSwitch = child.GetComponent<IPropertySwitch>();
            }
            if (objSwitch != null) {
              // Each child with a switch component gets a node with the current node as
              // its parent.
              children.Push(new SwitchTreeNode(child,
                                               objSwitch,
                                               new SwitchTreeNodeRef(this)));
            }
            else {
              // This node will "inherit" any grand-children nodes whose parents are
              // not _themselves_ switches as direct "children" in the switch tree.
              stack.Push(child);
            }
          }
        }
      }
      finally {
        stack.Clear();
        Pool<Stack<Transform>>.Recycle(stack);
      }
    }
  }

  [Serializable]
  public class SwitchTree {

    [SerializeField]
    private SwitchTreeNode root;

    public SwitchTree(Transform transform) {
      var objSwitch = transform.GetComponent<IPropertySwitch>();
      if (objSwitch == null) {
        throw new System.InvalidOperationException("Cannot build a Switch Tree for "
                                                 + "a Transform that is not itself a "
                                                 + "switch.");
      }

      root = new SwitchTreeNode(transform, objSwitch, null);
    }

    public int NodeCount {
      get { return root.numChildren + 1; }
    }

    public void SwitchTo(string nodeName) {
      // We have to traverse the whole tree because we don't know where the node matching
      // nodeName resides. This is fine, because the contract of the tree is to maintain
      // every node's state anyway, not just the ones that are currently activating or
      // deactivating.

      // TODO: DELETE THESE
      var activeNodeChain = Pool<Stack<SwitchTreeNodeRef>>.Spawn();
      var nonActiveNodes = Pool<List<SwitchTreeNodeRef>>.Spawn();

      var visitedNodes = Pool<HashSet<SwitchTreeNode>>.Spawn();
      var curNodeRef = Pool<SwitchTreeNodeRef>.Spawn();
      curNodeRef.node = root;
      SwitchTreeNode activeNode;
      try {
        while (curNodeRef != null) {

          var node = curNodeRef.node;
          if (!visitedNodes.Contains(node)) {
            // Visiting a new node.
            visitedNodes.Add(node);

            // TODO: DELETE
            // var nodeRef = Pool<SwitchTreeNodeRef>.Spawn();
            // nodeRef.node = node;
            // activeNodeChain.Push(nodeRef);

            if (node.transform.name.Equals(nodeName)) {
              // We've found the node we want active.
              activeNode = node;
            }
          }

          // Go deeper into children if there are any.
          bool goingDown = false;
          foreach (var child in node.children) {
            if (!visitedNodes.Contains(child)) {
              curNodeRef.node = child;
              goingDown = true;
              break;
            }
          }
          if (goingDown) { continue; }
          else {
            // Head back up to the parent node.
            curNodeRef = node.parent;
          }
        }
      }
      finally {
        Pool<SwitchTreeNodeRef>.Recycle(curNodeRef);

        visitedNodes.Clear();
        Pool<HashSet<SwitchTreeNode>>.Recycle(visitedNodes);

        foreach (var nodeRef in activeNodeChain) {
          Pool<SwitchTreeNodeRef>.Recycle(nodeRef);
        }

        activeNodeChain.Clear();
        Pool<Stack<SwitchTreeNodeRef>>.Recycle(activeNodeChain);

        nonActiveNodes.Clear();
        Pool<List<SwitchTreeNodeRef>>.Recycle(nonActiveNodes);
      }
    }
  }

}