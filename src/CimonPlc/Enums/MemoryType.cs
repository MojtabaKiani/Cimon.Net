namespace CimonPlc.Enums
{
    public enum MemoryType
    {
        /// <summary>
        /// M (Internal Memory)
        ///     This is used not to output but to configure a logic circuit.
        /// </summary>
        M,
        /// <summary>
        ///  X (Input)
        ///     This is the input part receiving data directly.
        /// </summary>
        X,
        /// <summary>
        /// Y (Output)
        ///     This is the output part transferring the result of an operation.
        /// </summary>
        Y,
        /// <summary>
        /// K (Keep)
        ///     This is used like M but is used as the device conserving the precious data
        ///     when the power is ON or the RUN starts. The data is conserved till the power
        ///     is ON again though it is OFF. It can be processed with “Data Clear” function
        ///     in the Loader to delete the data.
        /// </summary>
        K,
        /// <summary>
        /// L (Link)
        ///     It is unable to output to outside directly for data link with upper device and
        ///     lower one. When the power is ON and the RUN starts, the part except the
        ///     device assigned to a parameter is deleted as 0 and there is no default nonvolatile device.
        ///     In case that this is not used for link and high-speed counter,
        ///     this can be used like M.
        /// </summary>
        L,
        /// <summary>
        /// F (Internal Flag)
        ///     This has the device having the operation state, setting type, card number,
        ///     system clock contact and user clock contact for a PLC. It is available to input
        ///     an instruction with only Operand.
        /// </summary>
        F,
        /// <summary>
        /// T (Timer)
        ///     There are the instructions of 5 types and the counting method is different
        ///     according to instruction. If input condition is realized, a timer will start to count.
        ///     And if timer reaches set time or 0, contact output is ON. The maximum set
        ///     value is FFFFh and the value can be expressed in decimal figure or in
        ///     hexadecimal figure.
        ///     ON Delay, Off Delay, Accumulation ON Delay, Monostable, Retriggerable
        /// </summary>
        T,
        /// <summary>
        /// C (Counter)
        ///     Counter counts at the rising edge of input condition and stop counting at reset
        ///     input to delete current value as 0 or to substitute it as set value. According to
        ///     the instructions of 4 types, counting method is different. The maximum set
        ///     value is FFFFh and the value can be expressed in decimal figure or in
        ///     hexadecimal figure.
        ///     Up Counter, Down Counter, Up/Down Counter, Ring Counter
        /// </summary>
        C,
        /// <summary>
        /// S (Step Control)
        ///     This, which is the relay for step control, is classified into the priority of Last-In
        ///     and the step control according to using the instructions(OUT, SET). This is
        ///     composed of 2-step instruction.The device except the one assigned to a
        ///     parameter when the power is ON and the RUN starts is deleted as the first step, 0
        /// </summary>
        S,
        /// <summary>
        /// D (Data Register)
        ///     This is used to store the internal data.It is available to read and write in 32-bit
        /// </summary>
        D

    }
}
