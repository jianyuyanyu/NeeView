﻿// Copyright (c) 2016-2017 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System.Runtime.Serialization;

namespace NeeView
{
    //
    public class ExporterProfile
    {
        public static ExporterProfile Current { get; private set; }

        public ExporterProfile()
        {
            Current = this;
        }

        public bool IsHintCloneDefault { get; set; } = true;
        public int QualityLevel { get; set; } = 80;
        public string ExportFolder { get; set; } = null;
        public bool IsEnableExportFolder { get; set; } = true;


        #region Memento

        //
        [DataContract]
        public class Memento
        {
            [DataMember]
            public bool IsHintCloneDefault { get; set; }

            [DataMember]
            public int QualityLevel { get; set; }

            [DataMember]
            public string ExportFolder { get; set; }

            [DataMember]
            public bool IsEnableExportFolder { get; set; }
        }

        //
        public Memento CreateMemento()
        {
            var memento = new Memento();
            memento.IsHintCloneDefault = this.IsHintCloneDefault;
            memento.QualityLevel = this.QualityLevel;
            memento.ExportFolder = this.ExportFolder;
            memento.IsEnableExportFolder = this.IsEnableExportFolder;
            return memento;
        }

        //
        public void Restore(Memento memento)
        {
            if (memento == null) return;
            this.IsHintCloneDefault = memento.IsHintCloneDefault;
            this.QualityLevel = memento.QualityLevel;
            this.ExportFolder = memento.ExportFolder;
            this.IsEnableExportFolder = memento.IsEnableExportFolder;
        }

        #endregion
    }
}
