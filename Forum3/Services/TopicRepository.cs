﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Forum3.Data;
using Forum3.Helpers;
using Forum3.ViewModels.Topics;
using Microsoft.AspNet.Http;
using Microsoft.Data.Entity;

namespace Forum3.Services {
	public class TopicRepository {
		private ApplicationDbContext _dbContext;
		private IHttpContextAccessor _httpContextAccessor;

		public TopicRepository(ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor) {
			_dbContext = dbContext;
			_httpContextAccessor = httpContextAccessor;
		}

		public async Task<TopicIndex> GetTopicIndexAsync(int skip, int take) {
			var messageRecords = await (from m in _dbContext.Messages
										where m.ParentId == null
										orderby m.LastReplyPosted descending
										select new TopicPreview {
											Id = m.Id,
											ShortPreview = m.ShortPreview,
											LastReplyId = m.LastReplyId ?? m.Id,
											LastReplyById = m.LastReplyById,
											LastReplyPostedDT = m.LastReplyPosted,
											Views = m.Views,
											Replies = m.Replies,
										})
									.Skip(skip).Take(take)
									.ToListAsync();

			return new TopicIndex {
				Skip = skip + take,
				Take = take,
				Topics = messageRecords
			};
		}

		public async Task<Topic> GetTopicAsync(int id, int currentPage, int skip, int take, bool jumpToLatest) {
			var topicFirstPost = _dbContext.Messages.SingleOrDefault(m => m.Id == id);

			if (topicFirstPost == null)
				throw new Exception("No topic found with that ID.");

			if (topicFirstPost.ParentId != null)
				throw new ChildMessageException(topicFirstPost.Id, topicFirstPost.ParentId);

			topicFirstPost.Views++;
			_dbContext.Entry(topicFirstPost).State = EntityState.Modified;
			_dbContext.SaveChanges();

			var currentUser = _httpContextAccessor.HttpContext.User;
			var isAdmin = currentUser.Identity.IsAuthenticated && currentUser.IsInRole("Admin");

			// TEMP FIX BECAUSE EF7 LEFT OUTER JOINS ARE BROKEN. http://stackoverflow.com/a/34211463/2621693

			//var messages = await (
			//	from m in _dbContext.Messages
			//	join im in _dbContext.Messages on m.ReplyId equals im.Id into Replies
			//	from r in Replies.DefaultIfEmpty()
			//	where m.Id == id || m.ParentId == id
			//	select new ViewModels.Messages.Message {
			//		Id = m.Id,
			//		ParentId = m.ParentId,
			//		ReplyId = m.ReplyId,
			//		ReplyBody = r?.DisplayBody,
			//		ReplyPreview = r?.LongPreview,
			//		ReplyPostedBy = r?.PostedByName,
			//		Body = m.DisplayBody,
			//		OriginalBody = record.m.OriginalBody,
			//		PostedByName = m.PostedByName,
			//		PostedById = m.PostedById,
			//		TimePostedDT = m.TimePosted,
			//		TimeEditedDT = m.TimeEdited,
			//		RecordTime = m.TimeEdited,
			//	}
			//).Skip(skip).Take(take).ToListAsync();

			//foreach (var message in messages) {
			//	message.TimePosted = message.TimePostedDT.ToPassedTimeString();
			//	message.TimeEdited = message.TimeEditedDT.ToPassedTimeString();
			//	message.CanEdit = isAdmin || (currentUser.Identity.IsAuthenticated && currentUser.GetUserId() == message.PostedById);
			//	message.CanDelete = isAdmin || (currentUser.Identity.IsAuthenticated && currentUser.GetUserId() == record.m.PostedById);
			//	message.CanReply = currentUser.Identity.IsAuthenticated;
			//	message.CanThought = currentUser.Identity.IsAuthenticated;
			//}

			var messages = await (from m in _dbContext.Messages
						   join im in _dbContext.Messages on m.ReplyId equals im.Id into Replies
						   from r in Replies.DefaultIfEmpty()
						   where m.Id == id || m.ParentId == id
						   select new { m, r }).ToListAsync();

			var topic = new Topic {
				Id = topicFirstPost.Id,
				TopicHeader = new TopicHeader {
					StartedById = topicFirstPost.PostedById,
					Subject = topicFirstPost.ShortPreview,
					Views = topicFirstPost.Views,
				},
				Messages = new System.Collections.Generic.List<ViewModels.Messages.Message>(),
				//Boards = new List<IndexBoard>(),
				//AssignedBoards = new List<IndexBoard>(),
				IsAuthenticated = currentUser.Identity.IsAuthenticated,
				CanManage = isAdmin || topicFirstPost.PostedById == currentUser.GetUserId(),
				CanInvite = isAdmin || topicFirstPost.PostedById == currentUser.GetUserId(),
				TotalPages = take == 0 || messages.Count == 0 ? 1 : Convert.ToInt32(Math.Ceiling((double)messages.Count / take)),
				CurrentPage = currentPage
			};

			// TEMP FIX - REMOVE THIS LOOP AND UNCOMMENT BLOCK ABOVE WHEN EF7 LOJ ARE FIXED.
			// MAKE SURE YOU INCLUDE CHANGES TO THIS LOOP IN BLOCK ABOVE TOO!!

			foreach (var record in messages)	{
				topic.Messages.Add(new ViewModels.Messages.Message {
					Id = record.m.Id,
					ParentId = record.m.ParentId,
					ReplyId = record.m.ReplyId,
					ReplyBody = record.r?.DisplayBody,
					ReplyPreview = record.r?.LongPreview,
					ReplyPostedBy = record.r?.PostedByName,
					Body = record.m.DisplayBody,
					OriginalBody = record.m.OriginalBody,
					PostedByName = record.m.PostedByName,
					PostedById = record.m.PostedById,
					TimePostedDT = record.m.TimePosted,
					TimeEditedDT = record.m.TimeEdited,
					RecordTime = record.m.TimeEdited,
					TimePosted = record.m.TimePosted.ToPassedTimeString(),
					TimeEdited = record.m.TimeEdited.ToPassedTimeString(),
					CanEdit = isAdmin || (currentUser.Identity.IsAuthenticated && currentUser.GetUserId() == record.m.PostedById),
					CanDelete = isAdmin || (currentUser.Identity.IsAuthenticated && currentUser.GetUserId() == record.m.PostedById),
					CanReply = currentUser.Identity.IsAuthenticated,
					CanThought = currentUser.Identity.IsAuthenticated
				});
			}

			return topic;
		}
	}
}
