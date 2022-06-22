using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SharpWrap2534.PassThruTypes
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
    };
    /// <summary>
    /// Baud rate values commonly used for protocols
    /// </summary>
    public enum BaudRate : uint
    {
        ISO9141_10400 = 10400,
        ISO9141_10000 = 10000,

        ISO14230_10400 = 10400,
        ISO14230_10000 = 10000,

        J1850PWM_41600 = 41600,
        J1850PWM_83300 = 83300,

        J1850VPW_10400 = 10400,
        J1850VPW_41600 = 41600,

        CAN_125000 = 125000,
        CAN_250000 = 250000,
        CAN_500000 = 500000,

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
    /// <summary>
    /// Config params for using a ConfigParam struct.
    /// </summary>
    public enum ConfigParamId : uint
    {
        DATA_RATE = 0x01,
        LOOPBACK = 0x03,
        NODE_ADDRESS = 0x04,
        NETWORK_LINE = 0x05,
        P1_MIN = 0x06,                      // Don't use
        P1_MAX = 0x07,
        P2_MIN = 0x08,                      // Don't use
        P2_MAX = 0x09,                      // Don't use
        P3_MIN = 0x0A,
        P3_MAX = 0x0B,                      // Don't use
        P4_MIN = 0x0C,
        P4_MAX = 0x0D,                      // Don't use

        W1 = 0x0E,
        W2 = 0x0F,
        W3 = 0x10,
        W4 = 0x11,
        W5 = 0x12,
        TIDLE = 0x13,
        TINIL = 0x14,
        TWUP = 0x15,
        PARITY = 0x16,
        BIT_SAMPLE_POINT = 0x17,
        SYNC_JUMP_WIDTH = 0x18,
        W0 = 0x19,
        T1_MAX = 0x1A,
        T2_MAX = 0x1B,
        T4_MAX = 0x1C,

        T5_MAX = 0x1D,
        ISO15765_BS = 0x1E,
        ISO15765_STMIN = 0x1F,
        DATA_BITS = 0x20,
        FIVE_BAUD_MOD = 0x21,
        BS_TX = 0x22,
        STMIN_TX = 0x23,
        T3_MAX = 0x24,
        ISO15765_WFT_MAX = 0x25,

        N_BR_MIN = 0x2A,
        ISO15765_PAD_VALUE = 0x2B,
        N_AS_MAX = 0x2C,
        N_AR_MAX = 0x2D,
        N_BS_MAX = 0x2E,
        N_CR_MAX = 0x2F,
        N_CS_MIN = 0x30,

        // J2534-2
        CAN_MIXED_FORMAT = 0x8000,
        J1962_PINS = 0x8001,
        SW_CAN_HS_DATA_RATE = 0x8010,
        SW_CAN_SPEEDCHANGE_ENABLE = 0x8011,
        SW_CAN_RES_SWITCH = 0x8012,
        ACTIVE_CHANNELS = 0x8020,
        SAMPLE_RATE = 0x8021,
        SAMPLES_PER_READING = 0x8022,
        READINGS_PER_MSG = 0x8023,
        AVERAGING_METHOD = 0x8024,
        SAMPLE_RESOLUTION = 0x8025,
        INPUT_RANGE_LOW = 0x8026,
        INPUT_RANGE_HIGH = 0x8027,

        // J2534-2 UART Echo Byte protocol parameters
        UEB_T0_MIN = 0x8028,

        UEB_T1_MAX = 0x8029,
        UEB_T2_MAX = 0x802A,
        UEB_T3_MAX = 0x802B,
        UEB_T4_MIN = 0x802C,
        UEB_T5_MAX = 0x802D,
        UEB_T6_MAX = 0x802E,
        UEB_T7_MIN = 0x802F,
        UEB_T7_MAX = 0x8030,
        UEB_T9_MIN = 0x8031,

        // Pin selection
        J1939_PINS = 0x803D,
        J1708_PINS = 0x803E,

        // J2534-2 J1939 config parameters
        J1939_T1 = 0x803F,
        J1939_T2 = 0x8040,
        J1939_T3 = 0x8041,
        J1939_T4 = 0x8042,
        J1939_BRDCST_MIN_DELAY = 0x8043,

        // J2534-2 TP2.0
        TP2_0_T_BR_INT = 0x8044,
        TP2_0_T_E = 0x8045,
        TP2_0_MNTC = 0x8046,
        TP2_0_T_CTA = 0x8047,
        TP2_0_MNCT = 0x8048,
        TP2_0_MNTB = 0x8049,
        TP2_0_MNT = 0x804A,
        TP2_0_T_WAIT = 0x804B,
        TP2_0_T1 = 0x804C,
        TP2_0_T3 = 0x804D,
        TP2_0_IDENTIFER = 0x804E,
        TP2_0_RXIDPASSIVE = 0x804F,
        TP1_6_T_E = 0x8051,
        TP1_6_MNTC = 0x8052,
        TP1_6_MNT = 0x8053,
        TP1_6_T1 = 0x8054,
        TP1_6_T2 = 0x8055,
        TP1_6_T3 = 0x8056,
        TP1_6_T4 = 0x8057,
        TP1_6_IDENTIFER = 0x8058,
        TP1_6_RXIDPASSIVE = 0x8059,
        TP2_0_ACK_DELAY = 0x805B,

        // J2534-2 Device Config parameters
        NON_VOLATILE_STORE_1 = 0xC001, /* use SCONFIG_LIST */
        NON_VOLATILE_STORE_2 = 0xC002,
        NON_VOLATILE_STORE_3 = 0xC003,
        NON_VOLATILE_STORE_4 = 0xC004,
        NON_VOLATILE_STORE_5 = 0xC005,
        NON_VOLATILE_STORE_6 = 0xC006,
        NON_VOLATILE_STORE_7 = 0xC007,
        NON_VOLATILE_STORE_8 = 0xC008,
        NON_VOLATILE_STORE_9 = 0xC009,
        NON_VOLATILE_STORE_10 = 0xC00A,
    };
    /// <summary>
    /// Contains infos for the type of connector to consume.
    /// </summary>
    public enum Connector : uint
    {
        ENTIRE_DEVICE = 0,
        J1962_CONNECTOR = 0x00000001,
        J1939_CONNECTOR = 0x00010000,
        J1708_CONNECTOR = 0x00010001
    }
    /// <summary>
    /// Select Type for Uint connections
    /// </summary>
    public enum SelectType : uint
    {
        READABLE_TYPE = 1
    }
    /// <summary>
    /// Flags for connecting on a new channel during a PassThruConnect
    /// </summary>
    public enum PassThroughConnect : uint
    {
        NO_CONNECT_FLAGS = 0x00000000,
        CAN_29BIT_ID = 0x00000100,
        ISO9141_NO_CHECKSUM = 0x00000200,
        NO_CHECKSUM = ISO9141_NO_CHECKSUM,
        CAN_ID_BOTH = 0x00000800,
        ISO9141_K_LINE_ONLY = 0x00001000,
        SNIFF_MODE = 0x10000000,
        LISTEN_ONLY_DT = SNIFF_MODE,
        ISO9141_FORD_HEADER = 0x20000000, 
        ISO9141_NO_CHECKSUM_DT = 0x40000000
    };
    /// <summary>
    /// RX Status flags for inbound messages
    /// </summary>
    public enum RxStatus
    {
        NO_RX_STATUS = 0x00000000,
        TX_MSG_TYPE = 0x00000001,
        START_OF_MESSAGE = 0x00000002,
        ISO15765_FIRST_FRAME = 0x00000002,
        RX_BREAK = 0x00000004,
        TX_DONE = 0x00000008,
        ISO15765_PADDING_ERROR = 0x00000010,
        ISO15765_ADDR_TYPE = 0x00000080,
        ISO15765_EXT_ADDR = 0x00000080,
        CAN_29BIT_ID = 0x00000100,
        SW_CAN_NS_RX = 0x00040000,
        SW_CAN_HS_RX = 0x00020000,
        SW_CAN_HV_RX = 0x00010000,

        // TP2.0
        CONNECTION_ESTABLISHED = 0x10000,
        CONNECTION_LOST = 0x20000,
    };
    /// <summary>
    /// TX flags for outbound messages.
    /// </summary>
    public enum TxFlags : uint
    {
        NO_TX_FLAGS = 0x00000000,
        ISO15765_FRAME_PAD = 0x00000040,
        CAN_29BIT_ID = 0x00000100,
        WAIT_P3_MIN_ONLY = 0x00000200,
        SW_CAN_HV_TX = 0x00000400,
        TP_NOACKREQ_MSG = 0x00020000,
        TP_SEQCOUNT_RESET = 0x00040000,
        SCI_MODE = 0x00400000,
        SCI_TX_VOLTAGE = 0x00800000,
        DT_PERIODIC_UPDATE = 0x10000000,
        DT_DO_NOT_USE_BITS = 0x43000000,
    };
    /// <summary>
    /// SParam configuration for lists of configuration objects 
    /// </summary>
    public enum SParamParameters : int
    {
        SERIAL_NUMBER = 0x00000001,
        J1850PWM_SUPPORTED = 0x00000002,
        J1850VPW_SUPPORTED = 0x00000003,
        ISO9141_SUPPORTED = 0x00000004,
        ISO14230_SUPPORTED = 0x00000005,
        CAN_SUPPORTED = 0x00000006,
        ISO15765_SUPPORTED = 0x00000007,
        SCI_A_ENGINE_SUPPORTED = 0x00000008,
        SCI_A_TRANS_SUPPORTED = 0x00000009,
        SCI_B_ENGINE_SUPPORTED = 0x0000000A,
        SCI_B_TRANS_SUPPORTED = 0x0000000B,
        SW_ISO15765_SUPPORTED = 0x0000000C,
        SW_CAN_SUPPORTED = 0x0000000D,
        GM_UART_SUPPORTED = 0x0000000E,
        UART_ECHO_BYTE_SUPPORTED = 0x0000000F,
        HONDA_DIAGH_SUPPORTED = 0x00000010,
        J1939_SUPPORTED = 0x00000011,
        J1708_SUPPORTED = 0x00000012,
        TP2_0_SUPPORTED = 0x00000013,
        J2610_SUPPORTED = 0x00000014,
        ANALOG_IN_SUPPORTED = 0x00000015,
        MAX_NON_VOLATILE_STORAGE = 0x00000016,
        SHORT_TO_GND_J1962 = 0x00000017,
        PGM_VOLTAGE_J1962 = 0x00000018,
        J1850PWM_PS_J1962 = 0x00000019,
        J1850VPW_PS_J1962 = 0x0000001A,
        ISO9141_PS_K_LINE_J1962 = 0x0000001B,
        ISO9141_PS_L_LINE_J1962 = 0x0000001C,
        ISO14230_PS_K_LINE_J1962 = 0x0000001D,
        ISO14230_PS_L_LINE_J1962 = 0x0000001E,
        CAN_PS_J1962 = 0x0000001F,
        ISO15765_PS_J1962 = 0x00000020,
        SW_CAN_PS_J1962 = 0x00000021,
        SW_ISO15765_PS_J1962 = 0x00000022,
        GM_UART_PS_J1962 = 0x00000023,
        UART_ECHO_BYTE_PS_J1962 = 0x00000024,
        HONDA_DIAGH_PS_J1962 = 0x00000025,
        J1939_PS_J1962 = 0x00000026,
        J1708_PS_J1962 = 0x00000027,
        TP2_0_PS_J1962 = 0x00000028,
        J2610_PS_J1962 = 0x00000029,
        J1939_PS_J1939 = 0x0000002A,
        J1708_PS_J1939 = 0x0000002B,
        ISO9141_PS_K_LINE_J1939 = 0x0000002C,
        ISO9141_PS_L_LINE_J1939 = 0x0000002D,
        ISO14230_PS_K_LINE_J1939 = 0x0000002E,
        ISO14230_PS_L_LINE_J1939 = 0x0000002F,
        J1708_PS_J1708 = 0x00000030,
        FT_CAN_SUPPORTED = 0x00000031,
        FT_ISO15765_SUPPORTED = 0x00000032,
        FT_CAN_PS_J1962 = 0x00000033,
        FT_ISO15765_PS_J1962 = 0x00000034,
        J1850PWM_SIMULTANEOUS = 0x00000035,
        J1850VPW_SIMULTANEOUS = 0x00000036,
        ISO9141_SIMULTANEOUS = 0x00000037,
        ISO14230_SIMULTANEOUS = 0x00000038,
        CAN_SIMULTANEOUS = 0x00000039,
        ISO15765_SIMULTANEOUS = 0x0000003A,
        SCI_A_ENGINE_SIMULTANEOUS = 0x0000003B,
        SCI_A_TRANS_SIMULTANEOUS = 0x0000003C,
        SCI_B_ENGINE_SIMULTANEOUS = 0x0000003D,
        SCI_B_TRANS_SIMULTANEOUS = 0x0000003E,
        SW_ISO15765_SIMULTANEOUS = 0x0000003F,
        SW_CAN_SIMULTANEOUS = 0x00000040,
        GM_UART_SIMULTANEOUS = 0x00000041,
        UART_ECHO_BYTE_SIMULTANEOUS = 0x00000042,
        HONDA_DIAGH_SIMULTANEOUS = 0x00000043,
        J1939_SIMULTANEOUS = 0x00000044,
        J1708_SIMULTANEOUS = 0x00000045,
        TP2_0_SIMULTANEOUS = 0x00000046,
        J2610_SIMULTANEOUS = 0x00000047,
        ANALOG_IN_SIMULTANEOUS = 0x00000048,
        PART_NUMBER = 0x00000049,
        CONNECT_MEDIA = 0x0100,
        KW82_SUPPORTED = 0x0101,
        KW82_SIMULTANEOUS = 0x0102,
        KW82_PS_J1962 = 0x0103
    };
}
