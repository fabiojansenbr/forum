﻿using Forum3.Annotations;
using Forum3.Contexts;
using Forum3.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Forum3.Controllers {
	using InputModels = Models.InputModels;
	using ViewModels = Models.ViewModels;

	public class Messages : ForumController {
		ApplicationDbContext DbContext { get; }
		MessageRepository MessageRepository { get; }
		SettingsRepository SettingsRepository { get; }
		IUrlHelper UrlHelper { get; }

		public Messages(
			ApplicationDbContext dbContext,
			MessageRepository messageRepository,
			SettingsRepository settingsRepository,
			IActionContextAccessor actionContextAccessor,
			IUrlHelperFactory urlHelperFactory
		) {
			DbContext = dbContext;
			MessageRepository = messageRepository;
			SettingsRepository = settingsRepository;
			UrlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
		}

		[HttpGet]
		public async Task<IActionResult> Create(int id = 0) {
			var board = await DbContext.Boards.SingleOrDefaultAsync(b => b.Id == id);

			if (board is null)
				throw new Exception($"A record does not exist with ID '{id}'");

			var viewModel = new ViewModels.Messages.CreateTopicPage {
				BoardId = id,
				CancelPath = Referrer
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Create(InputModels.MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageRepository.CreateTopic(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = new ViewModels.Messages.CreateTopicPage() {
				BoardId = input.BoardId,
				Body = input.Body
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Edit(int id) {
			var record = await DbContext.Messages.SingleOrDefaultAsync(m => m.Id == id);

			if (record is null)
				throw new Exception($"A record does not exist with ID '{id}'");

			var viewModel = new ViewModels.Messages.EditMessagePage {
				Id = id,
				Body = record.OriginalBody,
				CancelPath = Referrer
			};

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> Edit(InputModels.MessageInput input) {
			if (ModelState.IsValid) {
				var serviceResponse = await MessageRepository.EditMessage(input);
				ProcessServiceResponse(serviceResponse);

				if (serviceResponse.Success)
					return RedirectFromService();
			}

			var viewModel = new ViewModels.Messages.CreateTopicPage() {
				Body = input.Body
			};

			return View(viewModel);
		}

		[HttpGet]
		public async Task<IActionResult> Delete(int id) {
			var serviceResponse = await MessageRepository.DeleteMessage(id);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[PreventRapidRequests]
		public async Task<IActionResult> AddThought(InputModels.ThoughtInput input) {
			var serviceResponse = await MessageRepository.AddThought(input);
			ProcessServiceResponse(serviceResponse);

			return RedirectFromService();
		}

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public IActionResult Admin(InputModels.Continue input = null) => View();

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public async Task<IActionResult> ProcessMessages(InputModels.Continue input) {
			if (string.IsNullOrEmpty(input.Stage)) {
				var totalSteps = MessageRepository.ProcessMessages();

				input = new InputModels.Continue {
					Stage = nameof(MessageRepository.ProcessMessages),
					CurrentStep = -1,
					TotalSteps = totalSteps
				};
			}
			else
				await MessageRepository.ProcessMessagesContinue(input);

			var viewModel = new ViewModels.Delay {
				ActionName = "Processing Messages",
				ActionNote = "Processing message text, loading external sites, replacing smiley codes.",
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Messages.Admin), nameof(Messages))
			};

			if (input.CurrentStep < input.TotalSteps) {
				input.CurrentStep++;
				viewModel.NextAction = UrlHelper.Action(nameof(Messages.ProcessMessages), nameof(Messages), input);
			}

			return View("Delay", viewModel);
		}

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public IActionResult ReprocessMessages(InputModels.Continue input) {
			if (string.IsNullOrEmpty(input.Stage)) {
				var totalSteps = MessageRepository.ReprocessMessages();

				input = new InputModels.Continue {
					Stage = nameof(MessageRepository.ReprocessMessages),
					CurrentStep = -1,
					TotalSteps = totalSteps
				};
			}
			else
				MessageRepository.ReprocessMessagesContinue(input);

			var viewModel = new ViewModels.Delay {
				ActionName = "Reprocessing Messages",
				ActionNote = "Processing message text, loading external sites, replacing smiley codes.",
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Messages.Admin), nameof(Messages))
			};

			if (input.CurrentStep < input.TotalSteps) {
				input.CurrentStep++;
				viewModel.NextAction = UrlHelper.Action(nameof(Messages.ReprocessMessages), nameof(Messages), input);
			}

			return View("Delay", viewModel);
		}

		[Authorize(Roles="Admin")]
		[HttpGet]
		public IActionResult RecountReplies(InputModels.Continue input) {
			if (string.IsNullOrEmpty(input.Stage)) {
				var totalSteps = MessageRepository.RecountReplies();

				input = new InputModels.Continue {
					Stage = nameof(MessageRepository.RecountReplies),
					CurrentStep = -1,
					TotalSteps = totalSteps
				};
			}
			else
				MessageRepository.RecountRepliesContinue(input);

			var viewModel = new ViewModels.Delay {
				ActionName = "Recounting Replies",
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Messages.Admin), nameof(Messages))
			};

			if (input.CurrentStep < input.TotalSteps) {
				input.CurrentStep++;
				viewModel.NextAction = UrlHelper.Action(nameof(Messages.RecountReplies), nameof(Messages), input);
			}

			return View("Delay", viewModel);
		}

		[Authorize(Roles = "Admin")]
		[HttpGet]
		public IActionResult RebuildParticipants(InputModels.Continue input) {
			if (string.IsNullOrEmpty(input.Stage)) {
				var totalSteps = MessageRepository.RebuildParticipants();

				input = new InputModels.Continue {
					Stage = nameof(MessageRepository.RebuildParticipants),
					CurrentStep = -1,
					TotalSteps = totalSteps
				};
			}
			else
				MessageRepository.RebuildParticipantsContinue(input);

			var viewModel = new ViewModels.Delay {
				ActionName = "Rebuilding participants",
				CurrentPage = input.CurrentStep,
				TotalPages = input.TotalSteps,
				NextAction = UrlHelper.Action(nameof(Messages.Admin), nameof(Messages))
			};

			if (input.CurrentStep < input.TotalSteps) {
				input.CurrentStep++;
				viewModel.NextAction = UrlHelper.Action(nameof(Messages.RebuildParticipants), nameof(Messages), input);
			}

			return View("Delay", viewModel);
		}
	}
}
