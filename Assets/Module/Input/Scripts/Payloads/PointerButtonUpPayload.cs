namespace Core.Module.Input
{
    public readonly struct PointerButtonUpPayload
    {
        public readonly int Button;

        public PointerButtonUpPayload(int button)
        {
            Button = button;
        }
    }
}
