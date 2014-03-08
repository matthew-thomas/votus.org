﻿using System;
using FakeItEasy;
using Votus.Core.Infrastructure.Data;
using Votus.Core.Tasks;
using Xunit;

namespace Votus.Testing.Unit.Core.Tasks
{
    public class TasksManagerTests
    {
        private readonly IVersioningRepository<Task>    _fakeRepository;
        private readonly TasksManager                   _tasksManager;

        public TasksManagerTests()
        {
            _fakeRepository = A.Fake<IVersioningRepository<Task>>();
            _tasksManager   = new TasksManager {
                TaskRepository = _fakeRepository
            };
        }
        
        [Fact]
        public
        async System.Threading.Tasks.Task 
        HandleAsync_CreateTaskCommand_TaskIsPersisted()
        {
            // Arrange
            var createTaskCommand = new CreateTaskCommand {
                NewTaskId = Guid.NewGuid()
            };
    
            // Act
            await _tasksManager.HandleAsync(createTaskCommand);

            // Assert
            A.CallTo(() => 
                _fakeRepository.SaveAsync(
                    A<Task>.That.Matches(persistedTask => persistedTask.Id == createTaskCommand.NewTaskId), 
                    A<int>.Ignored
                )
            ).MustHaveHappened();
        }

        [Fact]
        public
        async System.Threading.Tasks.Task
        HandleAsync_VoteTaskCompletedCommand_TaskIsSaved()
        {
            // Arrange
            var task                     = new Task                     { Id     = Guid.NewGuid() };
            var voteTaskCompletedCommand = new VoteTaskCompletedCommand { TaskId = task.Id        };

            A.CallTo(() => 
                _fakeRepository.GetAsync<Task>(voteTaskCompletedCommand.TaskId)
            ).ReturnsCompletedTask(task);

            // Act
            await _tasksManager.HandleAsync(voteTaskCompletedCommand);

            // Assert
            A.CallTo(() =>
                _fakeRepository.SaveAsync(
                    A<Task>.That.Matches(persistedTask => persistedTask.Id == task.Id),
                    A<int>.Ignored
                )
            ).MustHaveHappened();
        }
    }
}
