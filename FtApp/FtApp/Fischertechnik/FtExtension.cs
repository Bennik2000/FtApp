using System;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace TXTCommunication.Fischertechnik.Txt
{
    /// <summary>
    /// Holds the values of one single controller.
    /// </summary>
    class FtExtension
    {
        public int ExtensionId { get; set; }

        public short[] InputValues { get; }
        public InputMode[] InputModes { get; }
        public bool[] InputIsDigital { get; }

        public int[] OutputValues { get; }
        public bool[] OutputModes { get; }
        public MotorDirection[] MotorDirections { get; }

        public ushort SoundIndex { get; set; }
        public ushort SountRepeatCount { get; set; }

        public FtExtension(int extensionId)
        {
            ExtensionId = extensionId;

            InputValues = new short[TxtInterface.UniversalInputs];
            InputModes = new InputMode[TxtInterface.UniversalInputs];
            InputIsDigital = new bool[TxtInterface.UniversalInputs];

            OutputValues = new int[TxtInterface.PwmOutputs];
            OutputModes = new bool[TxtInterface.MotorOutputs];
            MotorDirections = new MotorDirection[TxtInterface.MotorOutputs];
        }

        public short GetInputValue(int index)
        {
            return InputValues[index];
        }
        internal void SetInputValue(int index, short value)
        {
            InputValues[index] = value;
        }

        public void SetInputMode(int index, InputMode mode, bool isDigital)
        {
            InputModes[index] = mode;
            InputIsDigital[index] = isDigital;
        }


        public int GetOutputValue(int outputIndex)
        {
            if (OutputModes[outputIndex / 2])
            {
                throw new InvalidOperationException($"O{outputIndex * 2} or O{outputIndex * 2 + 1} is a motor");
            }
            return OutputValues[outputIndex];
        }
        public void SetOutputValue(int outputIndex, int value)
        {
            if (OutputModes[outputIndex / 2])
            {
                throw new InvalidOperationException($"O{outputIndex * 2} or O{outputIndex * 2 + 1} is a motor");
            }

            if (value < 0 || value > 512)
            {
                throw new InvalidOperationException($"The value must be between 0 and 512. Value is {value}");
            }

            OutputValues[outputIndex] = value;
        }

        public MotorDirection GetMotorDirection(int motorIndex)
        {
            if (!OutputModes[motorIndex])
            {
                throw new InvalidOperationException($"O{motorIndex*2} or O{motorIndex*2+1} is not a motor");
            }

            return MotorDirections[motorIndex];
        }
        public void SetMotorDirection(int motorIndex, MotorDirection value)
        {
            if (!OutputModes[motorIndex])
            {
                throw new InvalidOperationException($"O{motorIndex * 2} or O{motorIndex * 2 + 1} is not a motor");
            }

            MotorDirections[motorIndex] = value;
            SetMotorValue(motorIndex, GetMotorValue(motorIndex));
        }

        public int GetMotorValue(int motorIndex)
        {
            if (!OutputModes[motorIndex])
            {
                throw new InvalidOperationException($"O{motorIndex * 2} or O{motorIndex * 2 + 1} is not a motor");
            }

            if (MotorDirections[motorIndex] == MotorDirection.Left)
            {
                return OutputValues[motorIndex * 2 + 1];
            }
            if (MotorDirections[motorIndex] == MotorDirection.Right)
            {
                return OutputValues[motorIndex * 2];
            }

            return 0;
        }
        public void SetMotorValue(int motorIndex, int value)
        {
            if (!OutputModes[motorIndex])
            {
                throw new InvalidOperationException($"O{motorIndex * 2} or O{motorIndex * 2 + 1} is not a motor");
            }

            if (MotorDirections[motorIndex] == MotorDirection.Left)
            {
                OutputValues[motorIndex*2] = 0;
                OutputValues[motorIndex*2 + 1] = value;
            }
            else if (MotorDirections[motorIndex] == MotorDirection.Right)
            {
                OutputValues[motorIndex * 2] = value;
                OutputValues[motorIndex * 2 + 1] = 0;
            }
        }

        public void SetOutputMode(int outputIndex, bool isMotor)
        {
            OutputModes[(int) Math.Floor((double)outputIndex / 2)] = isMotor;
        }

        public void ResetValues()
        {
            for (int i = 0; i < InputValues.Length; i++)
            {
                InputValues[i] = 0;
            }

            for (int i = 0; i < OutputValues.Length; i++)
            {
                OutputValues[i] = 0;
            }
        }
    }
}
