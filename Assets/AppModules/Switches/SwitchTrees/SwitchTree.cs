using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  [Serializable]
  public class SwitchTree {

    #region NodeRef & Node

    public class NodeRef {
      public Node node;

      public NodeRef(Node node) {
        this.node = node;
      }

      public NodeRef() { }
    }
    
    public struct Node {

      /// <summary>
      /// The serialized MonoBehaviour for this Node represents both its Transform
      /// (via its transform property) and its IPropertySwitch (because the MonoBehaviour
      /// itself must implement IPropertySwitch).
      /// </summary>
      MonoBehaviour switchBehaviour;

      public Transform transform {
        get { return switchBehaviour.transform; }
      }
      public IPropertySwitch objSwitch {
        get { return switchBehaviour as IPropertySwitch; }
      }
      
      public NodeRef     parent;
      public List<Node>  children;

      public int treeDepth;

      /// <summary>
      /// The number of this node's children and grandchildren.
      /// </summary>
      public int numAllChildren;

      public int numChildren { get { return children.Count; } }

      public bool hasChildren { get { return children.Count > 0; } }

      public bool hasParent { get { return parent != null; } }

      public bool hasSibling { get { return hasParent && parent.node.numChildren > 1; } }

      public bool hasNextSibling {
        get {
          return hasParent
              && parent.node.GetIndexOfChild(this) != parent.node.numChildren - 1;
        }
      }

      public bool hasPrevSibling {
        get {
          return hasParent
              && parent.node.GetIndexOfChild(this) != 0;
        }
      }

      public Node(MonoBehaviour switchBehaviour,
                  NodeRef parent = null,
                  int treeDepth = 0) {
        this.switchBehaviour = switchBehaviour;

        this.parent = parent;
        this.treeDepth = treeDepth;

        children = new List<Node>();
        numAllChildren = 0;
        constructChildren();
      }

      public int GetIndexOfChild(Node child) {
        return children.IndexOf(child);
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
                // Each child with a switch component gets a node with the current node
                // as its parent.
                var newChild = new Node((objSwitch as MonoBehaviour),
                                        new NodeRef(this),
                                        this.treeDepth + 1);
                children.Add(newChild);
                this.numAllChildren += 1 + newChild.numAllChildren;
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

    #endregion

    private Node root;

    private bool treeReady;

    /// <summary>
    /// This is the sole object Unity serializes to serialize the switch tree; the rest
    /// is generated in OnAfterDeserialize.
    /// </summary>
    [SerializeField]
    public MonoBehaviour rootSwitchBehaviour;

    public SwitchTree(Transform transform) {
      var objSwitch = transform.GetComponent<IPropertySwitch>();
      if (objSwitch == null) {
        throw new System.InvalidOperationException("Cannot build a Switch Tree for "
                                                 + "a Transform that is not itself a "
                                                 + "switch.");
      }

      rootSwitchBehaviour = (objSwitch as MonoBehaviour);
      treeReady = false;
    }

    public int NodeCount {
      get {
        ensureTreeReady();
        return root.numAllChildren + 1;
      }
    }

    private void ensureTreeReady() {
      if (!treeReady) {
        initTree();
      }
    }

    private void initTree() {
      root = new Node(rootSwitchBehaviour, null);
    }

    /// <summary>
    /// Traverses the tree, deactivating all switch pathways that do not lead to the
    /// switch node identified by this nodeName, then activating all switches along the
    /// path to the switch node identified by this nodeName, but no deeper. Switches that
    /// are children of the named node are also deactivated.
    /// </summary>
    public void SwitchTo(string nodeName, bool immediately = false) {
      ensureTreeReady();

      // We have to traverse the whole tree because we don't know where the node matching
      // nodeName resides. This is fine, because the contract of the tree is to maintain
      // every node's state anyway, not just the ones that are currently activating or
      // deactivating.

      var activeNodeChain = Pool<Stack<Node>>.Spawn();
      var visitedNodes = Pool<HashSet<Node>>.Spawn();
      var tempNodeRef = Pool<NodeRef>.Spawn();
      var curNodeRef = tempNodeRef;
      curNodeRef.node = root;

      // This dictionary allows us to reverse-breadth-first traverse all nodes once;
      // we build it during the depth-first traversal.
      var nodesAtDepthLevel = Pool<Dictionary<int, List<Node>>>.Spawn();
      int curDepth = 0, largestDepth = 0;

      Node activeNode;
      try {
        // Depth-first traversal.
        while (curNodeRef != null) {
          var node = curNodeRef.node;
          
          // If visiting a new node, check if it's the desired active node.
          if (!visitedNodes.Contains(node)) {
            visitedNodes.Add(node);

            // We also construct a depth-first stack of all nodes, so we
            // can deactivate nodes in a predictable (reverse-depth-first) order
            // post-traversal.
            List<Node> nodes = null;
            if (!nodesAtDepthLevel.TryGetValue(curDepth, out nodes)) {
              nodesAtDepthLevel[curDepth] = nodes = Pool<List<Node>>.Spawn();
            }
            nodes.Add(node);

            if (node.transform.name.Equals(nodeName)) {
              // We've found the node we want active.
              activeNode = node;
              buildNodeChain(activeNode, activeNodeChain);
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
          if (goingDown) {
            // Heading down into the unvisited child.
            curDepth += 1;
            if (curDepth > largestDepth) {
              largestDepth = curDepth;
            }
            continue;
          }
          else {
            // Head back up to the parent node.
            curDepth -= 1;
            curNodeRef = node.parent;
          }
        }

        // After traversal, we have a per-depth-level dictionary of all nodes and a
        // Stack of the node chain we desire to activate.
        //
        // Deeper nodes should deactivate before their parent nodes. Nodes in the chain
        // we desire active should not deactivate, and if they are not already active,
        // they should activate from the root down.
        if (nodesAtDepthLevel.Count == 0) return;
        List<Node> depthNodes;
        for (int d = largestDepth; d >= 0; d--) {
          depthNodes = nodesAtDepthLevel[d];

          foreach (var node in depthNodes) {
            if (node.objSwitch.GetIsOnOrTurningOn()
                && !activeNodeChain.Contains(node)) {
              turnOff(node, immediately);
            }
          }
        }
        while (activeNodeChain.Count > 0) {
          var node = activeNodeChain.Pop();
          if (node.objSwitch.GetIsOffOrTurningOff()) {
            turnOn(node, immediately);
          }
        }
      }
      finally {
        Pool<NodeRef>.Recycle(tempNodeRef);

        visitedNodes.Clear();
        Pool<HashSet<Node>>.Recycle(visitedNodes);

        foreach (var depthNodesPair in nodesAtDepthLevel) {
          var nodes = depthNodesPair.Value;
          nodes.Clear();
          Pool<List<Node>>.Recycle(nodes);
        }
        nodesAtDepthLevel.Clear();
        Pool<Dictionary<int, List<Node>>>.Recycle(nodesAtDepthLevel);

        activeNodeChain.Clear();
        Pool<Stack<Node>>.Recycle(activeNodeChain);
      }
    }

    /// <summary>
    /// Fills the node stack with the node-parent chain such that
    /// the first Pop() produces the highest parent node and further Pops()
    /// produce children down the chain, ending with (and including) the
    /// starting node.
    /// </summary>
    private static void buildNodeChain(Node node, Stack<Node> nodeStack) {
      nodeStack.Clear();

      var curNode = node;
      while (true) {
        nodeStack.Push(curNode);

        if (curNode.hasParent) {
          curNode = curNode.parent.node;
        }
        else {
          break;
        }
      }
    }

    private static void turnOn(Node node, bool immediately) {
      if (Application.isPlaying && !immediately) {
        node.objSwitch.On();
      }
      else {
        node.objSwitch.OnNow();
      }
    }

    private static void turnOff(Node node, bool immediately) {
      if (Application.isPlaying && !immediately) {
        node.objSwitch.Off();
      }
      else {
        node.objSwitch.OffNow();
      }
    }

    /// <summary>
    /// Returns an enumerator that traverses the switch tree depth-first. You must
    /// provide a HashSet of SwitchTree.Node objects for the enumerator to track which
    /// nodes it has visited already without allocating. (You can also use this, as long
    /// as you don't modify it mid-traversal.)
    /// </summary>
    public DepthFirstEnumerator Traverse(HashSet<Node> visitedNodesCache) {
      return new DepthFirstEnumerator(this, visitedNodesCache);
    }

    public struct DepthFirstEnumerator : IQueryOp<Node> {
      private Maybe<Node> maybeCurNode;
      private HashSet<Node> visitedNodes;
      private SwitchTree tree;

      public DepthFirstEnumerator(SwitchTree tree, HashSet<Node> useToTrackVisitedNodes) {
        useToTrackVisitedNodes.Clear();

        this.tree = tree;
        maybeCurNode = Maybe.None;
        visitedNodes = useToTrackVisitedNodes;
      }

      public DepthFirstEnumerator GetEnumerator() { return this; }
      public Node Current { get { return maybeCurNode.valueOrDefault; } }

      public bool MoveNext() {
        if (!maybeCurNode.hasValue) {
          maybeCurNode = Maybe.Some(tree.root);
          return true;
        }

        var node = maybeCurNode.valueOrDefault;
        visitedNodes.Add(node);

        bool goingDown = false;
        foreach (var child in node.children) {
          if (!visitedNodes.Contains(child)) {
            maybeCurNode = Maybe.Some(child);
            goingDown = true;
          }
        }
        if (goingDown) {
          // We've already set maybeCurNode with the child node.
          return true;
        }
        else if (node.hasParent) {
          maybeCurNode = Maybe.Some(node.parent.node);
          return MoveNext();
        }
        else {
          return false;
        }
      }

      public bool TryGetNext(out Node t) {
        bool hasNext = MoveNext();
        if (hasNext) {
          t = Current;
          return true;
        }
        else {
          t = default(Node);
          return false;
        }
      }

      public void Reset() {
        visitedNodes.Clear();
        maybeCurNode = Maybe.None;
      }

      public QueryWrapper<Node, DepthFirstEnumerator> Query() {
        return new QueryWrapper<Node, DepthFirstEnumerator>(this);
      }

    }

  }

}