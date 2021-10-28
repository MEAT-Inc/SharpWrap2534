using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBoxInvoker.PassThruLogic.SupportingLogic
{
    // This is a collection of SAE J2534 Enums needed to configure values for API Commands.

    /// <summary>
    /// Protocol ID Values to connect with
    /// </summary>
    public enum ProtocolId : uint
    {
        J1850VPW = 0x01,
        J1850PWM = 0x02,
        ISO9141 = 0x03,
        ISO14230 = 0x04,
        CAN = 0x05,
        ISO15765 = 0x06,
        SCI_A_ENGINE = 0x07,
        SCI_A_TRANS = 0x08,
        SCI_B_ENGINE = 0x09,
        SCI_B_TRANS = 0x0A,

        J1850VPW_PS = 0x8000, // Not supported
        J1850PWM_PS = 0x8001, // Not supported
        ISO9141_PS = 0x8002, // Not supported
        ISO14230_PS = 0x8003, // Not supported
        CAN_PS = 0x8004,
        ISO15765_PS = 0x8005,
        J2610_PS = 0x8006, // Not supported
        SW_ISO15765_PS = 0x8007,
        SW_CAN_PS = 0x8008,
        GM_UART_PS = 0x8009, // Not supported (YET)
        UART_ECHO_BYTE_PS = 0x800A,
        HONDA_DIAGH_PS = 0x800B, // Not supported
        J1939_PS = 0x800C, // Not supported
        J1708_PS = 0x800D,
        TP2_0_PS = 0x800E, // Not supported
        FT_CAN_PS = 0x800F, // Not supported
        FT_ISO15765_PS = 0x8010, // Not supported
        TP1_6_PS = 0x8014, // Not supported
        CANFD_PS = 0x801F, // Not supported
        ANALOG_IN_1 = 0xC000,
        ANALOG_IN_2 = 0xC001, // Supported on AVIT only
        ANALOG_IN_32 = 0xC01F,  // Not supported
        DT_ANALOG_IN_1 = 0x800B, // Old Drew Tech Analogs do not use in new designs
        DT_ANALOG_IN_2 = 0x800C, // Old Drew Tech Analogs do not use in new designs
        DT_J1708_PS = 0x10000001, // Old Drew Tech J1708 do not use in new designs
        DT_CEC1_PS = 0x10000002, // supported in TVIT
        DT_KW82_PS = 0x10000003, // supported in GM II, CarDAQ-M mega K
        DT_TP1_6_PS = 0x10000004,
    };
    /// <summary>
    /// Baud rate values commonly used for protocols
    /// </summary>
    public enum BaudRate : uint
    {
        ISO9141 = 10400,
        ISO9141_10400 = 10400,
        ISO9141_10000 = 10000,

        ISO14230 = 10400,
        ISO14230_10400 = 10400,
        ISO14230_10000 = 10000,

        J1850PWM = 41600,
        J1850PWM_41600 = 41600,
        J1850PWM_83300 = 83300,

        J1850VPW = 10400,
        J1850VPW_10400 = 10400,
        J1850VPW_41600 = 41600,

        CAN = 500000,
        CAN_125000 = 125000,
        CAN_250000 = 250000,
        CAN_500000 = 500000,

        ISO15765 = 500000,
        ISO15765_125000 = 125000,
        ISO15765_250000 = 250000,
        ISO15765_500000 = 500000
    }
    /// <summary>
    /// IOCTL IDs for the IOCTl command.
    /// </summary>
    public enum IoctlId : uint
    {
        GET_CONFIG = 0x01,
        SET_CONFIG = 0x02,
        READ_PIN_VOLTAGE = 0x03,
        FIVE_BAUD_INIT = 0x04,
        FAST_INIT = 0x05,

        //NOT_USED = 0x06,
        CLEAR_TX_BUFFER = 0x07,

        CLEAR_RX_BUFFER = 0x08,
        CLEAR_PERIODIC_MSGS = 0x09,
        CLEAR_MSG_FILTERS = 0x0A,
        CLEAR_FUNCT_MSG_LOOKUP_TABLE = 0x0B,
        ADD_TO_FUNCT_MSG_LOOKUP_TABLE = 0x0C,
        DELETE_FROM_FUNCT_MSG_LOOKUP_TABLE = 0x0D,
        READ_PROG_VOLTAGE = 0x0E,

        SW_CAN_HS = 0x00008000,
        SW_CAN_NS = 0x00008001,
        SET_POLL_RESPONSE = 0x00008002,
        BECOME_MASTER = 0x00008003,
        START_REPEAT_MESSAGE = 0x00008004,
        QUERY_REPEAT_MESSAGE = 0x00008005,
        STOP_REPEAT_MESSAGE = 0x00008006,
        GET_DEVICE_CONFIG = 0x00008007,
        SET_DEVICE_CONFIG = 0x00008008,
        PROTECT_J1939_ADDR = 0x00008009,
        REQUEST_CONNECTION = 0x0000800A,
        TEARDOWN_CONNECTION = 0x0000800B,
        GET_DEVICE_INFO = 0x0000800C,
        GET_PROTOCOL_INFO = 0x0000800D,
        READ_V_J1962PIN1 = 0x0000800E,

        READ_CH1_VOLTAGE = 0x10000, // input = Num values, output = ptr long[]

        READ_CH2_VOLTAGE = 0x10001, // input = Num values, output = ptr long[]
        READ_CH3_VOLTAGE = 0x10002, // input = Num values, output = ptr long[]
        READ_CH4_VOLTAGE = 0x10003, // input = Num values, output = ptr long[]
        READ_CH5_VOLTAGE = 0x10004, // input = Num values, output = ptr long[]
        READ_CH6_VOLTAGE = 0x10005, // input = Num values, output = ptr long[]

        // DT CarDAQ2534 Ioctl for reading block of data
        READ_ANALOG_CH1 = 0x10010, // input = NULL, output = ptr long

        READ_ANALOG_CH2 = 0x10011, // input = NULL, output = ptr long
        READ_ANALOG_CH3 = 0x10012, // input = NULL, output = ptr long
        READ_ANALOG_CH4 = 0x10013, // input = NULL, output = ptr long
        READ_ANALOG_CH5 = 0x10014, // input = NULL, output = ptr long
        READ_ANALOG_CH6 = 0x10015, // input = NULL, output = ptr long
        READ_TIMESTAMP = 0x10100, // input = NULL, output = ptr long
        READ_CHANNEL_ERRORS = 0x10101, // input = Num values, output = ptr long[]
    };
    /// <summary>
    /// Filter definitions for J2534 Filter setup.
    /// </summary>
    public enum FilterDef : uint
    {
        PASS_FILTER = 0x00000001,
        BLOCK_FILTER = 0x00000002,
        FLOW_CONTROL_FILTER = 0x00000003,
    };
    /// <summary>
    /// J2534 Error enum values.
    /// </summary>
    public enum J2534Err
    {
        STATUS_NOERROR = 0x00,
        ERR_NOT_SUPPORTED = 0x01,
        ERR_INVALID_CHANNEL_ID = 0x02,
        ERR_INVALID_PROTOCOL_ID = 0x03,
        ERR_NULL_PARAMETER = 0x04,
        ERR_INVALID_IOCTL_VALUE = 0x05,
        ERR_INVALID_FLAGS = 0x06,
        ERR_FAILED = 0x07,
        ERR_DEVICE_NOT_CONNECTED = 0x08,
        ERR_TIMEOUT = 0x09,
        ERR_INVALID_MSG = 0x0A,
        ERR_INVALID_TIME_INTERVAL = 0x0B,
        ERR_EXCEEDED_LIMIT = 0x0C,
        ERR_INVALID_MSG_ID = 0x0D,
        ERR_DEVICE_IN_USE = 0x0E,
        ERR_INVALID_IOCTL_ID = 0x0F,
        ERR_BUFFER_EMPTY = 0x10,
        ERR_BUFFER_FULL = 0x11,
        ERR_BUFFER_OVERFLOW = 0x12,
        ERR_PIN_INVALID = 0x13,
        ERR_CHANNEL_IN_USE = 0x14,
        ERR_MSG_PROTOCOL_ID = 0x15,
        ERR_INVALID_FILTER_ID = 0x16,
        ERR_NO_FLOW_CONTROL = 0x17,
        ERR_NOT_UNIQUE = 0x18,
        ERR_INVALID_BAUDRATE = 0x19,
        ERR_INVALID_DEVICE_ID = 0x1A
    }
}
