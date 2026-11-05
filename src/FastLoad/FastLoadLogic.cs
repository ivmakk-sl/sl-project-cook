namespace ProjectCook
{
    // Game-free counter of the hold of the cooking window refreshes during a click on Load: between Begin and End (or
    // Abort) each refresh is held and counted, and the click ends with one refresh when at least one was held. This
    // file must not use any game or BepInEx type, because the unit tests compile it alone.
    public sealed class FastLoadLogic
    {
        private int _depth;

        // The refreshes held since the outer Begin.
        public int Held { get; private set; }

        // True between the outer Begin and its End (or Abort).
        public bool InClick => _depth > 0;

        public void Begin() => _depth++;

        // True while a click runs: the refresh is held and counted.
        public bool TryHold()
        {
            if (_depth <= 0) return false;
            Held++;
            return true;
        }

        // Ends a click. True only at the outer end when a refresh was held: the caller then sends one refresh.
        public bool End()
        {
            if (_depth <= 0) return false;
            if (--_depth > 0) return false;
            bool any = Held > 0;
            Held = 0;
            return any;
        }

        // Ends each open click after an exception. True when a refresh was held.
        public bool Abort()
        {
            bool any = Held > 0;
            _depth = 0;
            Held = 0;
            return any;
        }
    }
}
