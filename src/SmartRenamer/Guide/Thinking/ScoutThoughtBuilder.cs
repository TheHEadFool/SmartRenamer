using System.Collections.Generic;
using SmartRenamer.Models;

namespace SmartRenamer.Guide.Thinking
{
    /// <summary>
    /// Builds Scout's thoughts from everything
    /// currently known about the project.
    /// </summary>
    public class ScoutThoughtBuilder
    {
        public List<ScoutThought> Build(ProjectContext context)
        {
            List<ScoutThought> thoughts = new();

            //
            // Scout always begins by understanding
            // the user's goal.
            //
            thoughts.Add(new ScoutThought
            {
                Priority = 100,
                NeedsUserInput = true,

                Thought =
                    "I don't yet understand what the user wants to accomplish.",

                Question =
                    "Before we begin, what would you like to accomplish with these files?"
            });

            //
            // Camera generated filenames.
            //
            if (context.ProjectType == "Photos")
            {
                thoughts.Add(new ScoutThought
                {
                    Priority = 90,
                    NeedsUserInput = true,

                    Thought =
                        "These appear to be photographs with camera-generated filenames.",

                    Question =
                        "Would you like to keep the existing filenames, make them easier to read, or replace them completely?"
                });
            }

            //
            // Downloads folder.
            //
            if (context.ProjectType == "Downloads")
            {
                thoughts.Add(new ScoutThought
                {
                    Priority = 80,
                    NeedsUserInput = true,

                    Thought =
                        "This appears to be a Downloads folder.",

                    Question =
                        "Would you like me to organize these files into folders while leaving the originals untouched?"
                });
            }

            //
            // Music collection.
            //
            if (context.ProjectType == "Music")
            {
                thoughts.Add(new ScoutThought
                {
                    Priority = 80,
                    NeedsUserInput = true,

                    Thought =
                        "This looks like a music collection.",

                    Question =
                        "Would you like your music organized by artist, album, genre, or something else?"
                });
            }

            //
            // Documents.
            //
            if (context.ProjectType == "Documents")
            {
                thoughts.Add(new ScoutThought
                {
                    Priority = 80,
                    NeedsUserInput = true,

                    Thought =
                        "This appears to be a document collection.",

                    Question =
                        "Would you like these organized by topic, date, client, or another method?"
                });
            }

            return thoughts;
        }
    }
}