﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.Azure.Cosmos.Query.Core.Metrics
{
    using System.Collections.Generic;
    using System.Linq;

    internal class IndexUtilizationInfoAccumulator
    {
        public IndexUtilizationInfoAccumulator(
            IEnumerable<SingleIndexUtilizationEntity> utilizedSingleIndexes,
            IEnumerable<SingleIndexUtilizationEntity> potentialSingleIndexes,
            IEnumerable<CompositeIndexUtilizationEntity> utilizedCompositeIndexes,
            IEnumerable<CompositeIndexUtilizationEntity> potentialCompositeIndexes)
        {
            this.UtilizedSingleIndexes = utilizedSingleIndexes;
            this.PotentialSingleIndexes = potentialSingleIndexes;
            this.UtilizedCompositeIndexes = utilizedCompositeIndexes;
            this.PotentialCompositeIndexes = potentialCompositeIndexes;
        }

        public IndexUtilizationInfoAccumulator()
        {
            this.UtilizedSingleIndexes = default;
            this.PotentialSingleIndexes = default;
            this.UtilizedCompositeIndexes = default;
            this.PotentialCompositeIndexes = default;
        }

        private IEnumerable<SingleIndexUtilizationEntity> UtilizedSingleIndexes { get; set; }
        private IEnumerable<SingleIndexUtilizationEntity> PotentialSingleIndexes { get; set; }
        private IEnumerable<CompositeIndexUtilizationEntity> UtilizedCompositeIndexes { get; set; }
        private IEnumerable<CompositeIndexUtilizationEntity> PotentialCompositeIndexes { get; set; }

        public void Accumulate(IndexUtilizationInfo indexUtilizationInfo)
        {
            this.UtilizedSingleIndexes = (this.UtilizedSingleIndexes ?? Enumerable.Empty<SingleIndexUtilizationEntity>()).Concat(indexUtilizationInfo.UtilizedSingleIndexes);
            this.PotentialSingleIndexes = (this.PotentialSingleIndexes ?? Enumerable.Empty<SingleIndexUtilizationEntity>()).Concat(indexUtilizationInfo.PotentialSingleIndexes);
            this.UtilizedCompositeIndexes = (this.UtilizedCompositeIndexes ?? Enumerable.Empty<CompositeIndexUtilizationEntity>()).Concat(indexUtilizationInfo.UtilizedCompositeIndexes);
            this.PotentialCompositeIndexes = (this.PotentialCompositeIndexes ?? Enumerable.Empty<CompositeIndexUtilizationEntity>()).Concat(indexUtilizationInfo.PotentialCompositeIndexes);
            return;
        }

        public IndexUtilizationInfo GetIndexUtilizationInfo()
        {
            return new IndexUtilizationInfo(
                utilizedSingleIndexes: this.UtilizedSingleIndexes.ToList(),
                potentialSingleIndexes: this.PotentialSingleIndexes.ToList(),
                utilizedCompositeIndexes: this.UtilizedCompositeIndexes.ToList(),
                potentialCompositeIndexes: this.PotentialCompositeIndexes.ToList());
        }
    }
}