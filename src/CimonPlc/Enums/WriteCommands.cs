namespace CimonPlc.Enums
{
    public enum WriteCommands
    {
        /// <summary>
        /// Write data to memory by the word.
        /// </summary>
        WordBlockWrite = 0x57,
        /// <summary>
        /// Write data to memory by the bit.
        /// </summary>
        BitBlockWrite = 0x77
    }
}
