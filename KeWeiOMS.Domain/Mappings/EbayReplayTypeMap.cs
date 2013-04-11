﻿//--------------------------------------------------------------------
// All Rights Reserved , Copyright (C)  , KeWei TECH, Ltd.
//--------------------------------------------------------------------

using System;
using System.Collections.Generic;
using FluentNHibernate.Mapping;

namespace KeWeiOMS.Domain
{

    /// <summary>
    /// EbayReplayTypeMap
    /// ebay回复人员
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
    public class EbayReplayTypeMap : ClassMap<EbayReplayType> 
    {
        public EbayReplayTypeMap()
        {
            Table("EbayReplay");
            Id(x => x.Id);
            Map(x => x.ReplayBy).Length(255);
            Map(x => x.ReplayAccount).Length(255);
            Map(x => x.CreateBy).Length(255);
            Map(x => x.CreateOn);
        }
    }
}
