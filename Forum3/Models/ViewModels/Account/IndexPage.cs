﻿using Forum3.Models.DataModels;
using System.Collections.Generic;

namespace Forum3.Models.ViewModels.Account {
	public class IndexPage {
		public List<IndexItem> IndexItems { get; set; } = new List<IndexItem>();
	}
}