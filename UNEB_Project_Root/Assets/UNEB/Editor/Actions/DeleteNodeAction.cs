﻿
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UNEB.Utility;

namespace UNEB
{
    // Each input is paired with 1 output.
    using InputToOutputPair = Pair<NodeInput, NodeOutput>;

    // Each output can have many inputs
    using OutputToInputsPair = Pair<NodeOutput, System.Collections.Generic.List<NodeInput>>;

    public class DeleteNodeAction : UndoableAction
    {
        private NodeGraph _canvas;
        private Node _nodeRemoved = null;

        private List<InputToOutputPair> _oldConnectedOutputs;
        private List<OutputToInputsPair> _oldConnectedInputs;

        // The node referenced can only be destroyed if the 
        // delete action has been done or redone.
        private bool _bCanDeleteNode = false;

        public DeleteNodeAction()
        {
            _oldConnectedOutputs = new List<InputToOutputPair>();
            _oldConnectedInputs = new List<OutputToInputsPair>();
        }

        public override bool Init()
        {
            return manager.window.state.selectedNode != null;
        }

        public override void Do()
        {
            _canvas = manager.window.graph;
            _nodeRemoved = manager.window.state.selectedNode;
            _canvas.Remove(_nodeRemoved);

            // Remember all the old outputs the inputs were connected to.
            foreach (var input in _nodeRemoved.Inputs) {

                if (input.HasOutputConnected()) {
                    _oldConnectedOutputs.Add(new InputToOutputPair(input, input.OutputConnection));
                }
            }

            // Remember all the old input connections that the outputs were connected to.
            foreach (var output in _nodeRemoved.Outputs) {

                if (output.InputCount != 0) {
                    _oldConnectedInputs.Add(new OutputToInputsPair(output, output.Inputs.ToList()));
                }
            }

            disconnectOldConnections();

            _bCanDeleteNode = true;
        }

        public override void Undo()
        {
            _canvas.nodes.Add(_nodeRemoved);
            reconnectOldConnections();

            _bCanDeleteNode = false;
        }

        public override void Redo()
        {
            _canvas.Remove(_nodeRemoved);
            disconnectOldConnections();

            _bCanDeleteNode = true;
        }

        private void disconnectOldConnections()
        {
            // For all the outputs for this node, remove all the connected inputs.
            foreach (var output in _nodeRemoved.Outputs) {
                output.RemoveAll();
            }

            // For all the inputs for this node, have their connected outputs disconnect.
            foreach (var input in _nodeRemoved.Inputs) {

                if (input.HasOutputConnected()) {
                    input.OutputConnection.Remove(input);
                }
            }
        }

        private void reconnectOldConnections()
        {
            // For all the remembered inputs (of this node) to output pairs, reconnect.
            foreach (InputToOutputPair inOutPair in _oldConnectedOutputs) {

                NodeInput input = inOutPair.item1;
                NodeOutput output = inOutPair.item2;

                output.Add(input);
            }

            // For all the remembered outputs (of this node) to inputs, reconnect.
            foreach (OutputToInputsPair outInsPair in _oldConnectedInputs) {

                NodeOutput output = outInsPair.item1;
                List<NodeInput> inputs = outInsPair.item2;

                foreach (var input in inputs) {
                    output.Add(input);
                }
            }
        }

        public override void Disable()
        {
            if (_bCanDeleteNode && _nodeRemoved) {
                ScriptableObject.DestroyImmediate(_nodeRemoved, true);
            }
        }
    }
}