using System;
using System.Collections.Generic;
using TopSpeed.Vehicles.Control;

namespace TopSpeed.Vehicles.Core
{
    internal abstract class CarBase
    {
        private ICarController _primaryController;
        private ICarController? _overrideController;
        private IControlArbiter _controlArbiter;
        private IReadOnlyList<ICarModifier> _modifiers;

        protected CarBase(ICarController primaryController)
        {
            _primaryController = primaryController ?? throw new ArgumentNullException(nameof(primaryController));
            _controlArbiter = new DefaultControlArbiter();
            _modifiers = Array.Empty<ICarModifier>();
        }

        // True while the game is auto-driving the car (e.g. the pit lane sequence) instead of the
        // player. Player-only aids like shift-on-demand stand down in that window.
        protected bool HasOverrideController => _overrideController != null;

        protected CarControlIntent ResolveControlIntent(in CarControlContext context)
        {
            var intent = _controlArbiter.ResolveIntent(_primaryController, _overrideController, context);
            for (var i = 0; i < _modifiers.Count; i++)
                intent = _modifiers[i].ApplyIntent(context, intent);
            return intent;
        }

        protected virtual void OnBeforeRun(float elapsed, in CarControlContext context, in CarControlIntent intent)
        {
        }

        protected virtual void OnAfterRun(float elapsed, in CarControlContext context, in CarControlIntent intent)
        {
        }

        protected virtual void OnStateChanged(CarState previousState, CarState currentState)
        {
        }

        protected void NotifyStateChanged(CarState previousState, CarState currentState)
        {
            if (previousState == currentState)
                return;
            OnStateChanged(previousState, currentState);
        }

        public void SetPrimaryController(ICarController controller)
        {
            _primaryController = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public void SetOverrideController(ICarController? controller)
        {
            _overrideController = controller;
        }

        public void SetControlArbiter(IControlArbiter arbiter)
        {
            _controlArbiter = arbiter ?? throw new ArgumentNullException(nameof(arbiter));
        }

        public void SetModifiers(IReadOnlyList<ICarModifier>? modifiers)
        {
            _modifiers = modifiers ?? Array.Empty<ICarModifier>();
        }
    }
}

