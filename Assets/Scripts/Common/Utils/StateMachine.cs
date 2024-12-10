using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;


/* MIT License

Copyright (c) 2020 johans2

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

Source: https://github.com/johans2/FSM
*/

namespace HFSM
{
    public abstract class StateMachine
    {
        private StateMachine currentSubState;
        private StateMachine defaultSubState;
        private StateMachine parent;

        private Dictionary<Type, StateMachine> subStates = new Dictionary<Type, StateMachine>();
        private Dictionary<int, StateMachine> transitions = new Dictionary<int, StateMachine>();

        public void EnterStateMachine()
        {
            OnEnter();
            if (currentSubState == null && defaultSubState != null)
            {
                currentSubState = defaultSubState;
            }
            currentSubState?.EnterStateMachine();
        }

        public void UpdateStateMachine()
        {
            OnUpdate();
            currentSubState?.UpdateStateMachine();
        }

        public void ExitStateMachine()
        {
            currentSubState?.ExitStateMachine();
            OnExit();
        }

        protected virtual void OnEnter() { }

        protected virtual void OnUpdate() { }

        protected virtual void OnExit() { }

        public void LoadSubState(StateMachine subState)
        {
            if (subStates.Count == 0)
            {
                defaultSubState = subState;
            }

            subState.parent = this;
            try
            {
                subStates.Add(subState.GetType(), subState);
            }
            catch (ArgumentException)
            {
                throw new DuplicateSubStateException($"State {GetType()} already contains a substate of type {subState.GetType()}");
            }

        }

        public void AddTransition(StateMachine from, StateMachine to, int trigger)
        {
            if (!subStates.TryGetValue(from.GetType(), out _))
            {
                throw new InvalidTransitionException($"State {GetType()} does not have a substate of type {from.GetType()} to transition from.");
            }

            if (!subStates.TryGetValue(to.GetType(), out _))
            {
                throw new InvalidTransitionException($"State {GetType()} does not have a substate of type {to.GetType()} to transition from.");
            }

            try
            {
                from.transitions.Add(trigger, to);
            }
            catch (ArgumentException)
            {
                throw new DuplicateTransitionException($"State {from} already has a transition defined for trigger {trigger}");
            }

        }

        public void SendTrigger(int trigger)
        {
            var root = this;
            while (root?.parent != null)
            {
                root = root.parent;
            }

            while (root != null)
            {
                if (root.transitions.TryGetValue(trigger, out StateMachine toState))
                {
                    root.parent?.ChangeSubState(toState);
                    return;
                }
                root = root.currentSubState;
            }

            throw new NeglectedTriggerException($"Trigger {trigger} was not consumed by any transition!");
        }

        private void ChangeSubState(StateMachine state)
        {
            currentSubState?.ExitStateMachine();
            var newState = subStates[state.GetType()];
            currentSubState = newState;
            newState.EnterStateMachine();
        }
    }

    [Serializable]
    internal class DuplicateTransitionException : Exception
    {
        public DuplicateTransitionException()
        {
        }

        public DuplicateTransitionException(string message) : base(message)
        {
        }

        public DuplicateTransitionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DuplicateTransitionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class NeglectedTriggerException : Exception
    {
        public NeglectedTriggerException()
        {
        }

        public NeglectedTriggerException(string message) : base(message)
        {
        }

        public NeglectedTriggerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NeglectedTriggerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class InvalidTransitionException : Exception
    {
        public InvalidTransitionException()
        {
        }

        public InvalidTransitionException(string message) : base(message)
        {
        }

        public InvalidTransitionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidTransitionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class DuplicateSubStateException : Exception
    {
        public DuplicateSubStateException()
        {
        }

        public DuplicateSubStateException(string message) : base(message)
        {
        }

        public DuplicateSubStateException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DuplicateSubStateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}