﻿//--------------------------------------------------------------------
// All Rights Reserved , Copyright (C)  , KeWei TECH, Ltd.
//--------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace KeWeiOMS.Domain
{

    /// <summary>
    /// MachinesOldType
    /// 设备表过去使用
    /// 
    /// 修改纪录
    /// 
    ///  版本：1.0  创建主键。
    /// 
    /// 版本：1.0
    /// 
    /// <author>
    /// <name></name>
    /// <date></date>
    /// </author>
    /// </summary>
    public class MachinesOldType
    {
        /// <summary>
        /// 主键
        /// </summary>
        public virtual int Id { get; set; }

        /// <summary>
        /// 设备编号
        /// </summary>
        public virtual String MachineCode { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public virtual String StatusOld { get; set; }

        /// <summary>
        /// 使用者
        /// </summary>
        public virtual String UserNameOld { get; set; }

        /// <summary>
        /// 开始使用时间
        /// </summary>
        public virtual DateTime StartDateOld { get; set; }

        /// <summary>
        /// 结束始用时间
        /// </summary>
        public virtual DateTime EndDateOld { get; set; }

    }
}