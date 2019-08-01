# SenseNet-Linked-Content
Various solutions for linked content handling in sensenet

# Idea behind it
These are content handlers to handle soft linking external urls or hard linking another trepository Content. Basically you can create link or virtual contents.

# How it works
1. install ctd for selected logic
1. deploy assembly 
1. create content with installed content type
1. set external url or reference another Content
1. with ContentLinkPlus fetch Content as usual other normal sensenet Contents, it should contain the referenced Content metas along with it's own fields
1. with softLink fetch as a usual Content's action, it will contain soft link to where appmodell Content has an url

# Additional logic
I added a solution for multiple site repositories to generate action link with the corresponding host, and an experimental addition to use server side rewrite logic.

I've used these solutions in several of my projects but now merged into one package and had minor refactor, and so are untested with this compilation.
