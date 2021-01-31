using System;
using System.Collections.Generic;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Models;
using SharpVk;
using Buffer = SharpVk.Buffer;

namespace ajiva.Systems.VulcanEngine.Systems
{
    public class ImageSystem : ComponentSystemBase<AImage>, IInit
    {
        public ImageView CreateImageView(Image image, Format format, ImageAspectFlags aspectFlags)
        {
            return Ecs.GetSystem<DeviceSystem>().Device!.CreateImageView(image, ImageViewType.ImageView2d, format, ComponentMapping.Identity, new()
            {
                AspectMask = aspectFlags,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1,
            });
        }

        public AImage CreateImageAndView(uint width, uint height, Format format, ImageTiling tiling, ImageUsageFlags usage, MemoryPropertyFlags properties, ImageAspectFlags aspectFlags)
        {
            var aImage = new AImage(true);
            var deviceSystem = Ecs.GetSystem<DeviceSystem>();
            var device = deviceSystem.Device!;

            aImage.Image = device.CreateImage(ImageType.Image2d, format, new(width, height, 1), 1, 1, SampleCountFlags.SampleCount1, tiling, usage, SharingMode.Exclusive, ArrayProxy<uint>.Null, ImageLayout.Undefined);

            var memRequirements = device.GetImageMemoryRequirements2(new()
            {
                Image = aImage.Image
            });

            aImage.Memory = device.AllocateMemory(memRequirements.MemoryRequirements.Size, deviceSystem.FindMemoryType(memRequirements.MemoryRequirements.MemoryTypeBits, properties), new()
            {
                Image = aImage.Image,
            });

            device.BindImageMemory2(new BindImageMemoryInfo
            {
                Image = aImage.Image,
                Memory = aImage.Memory,
                MemoryOffset = 0,
            });

            aImage.View = CreateImageView(aImage.Image, format, aspectFlags);

            return aImage;
        }

        public Format FindDepthFormat()
        {
            return FindSupportedFormat(new[]
                {
                    Format.D32SFloat, Format.D32SFloatS8UInt, Format.D24UNormS8UInt
                },
                ImageTiling.Optimal,
                FormatFeatureFlags.DepthStencilAttachment
                //VK_FORMAT_FEATURE_DEPTH_STENCIL_ATTACHMENT_BIT
            );
        }

        private Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
        {
            foreach (var format in candidates)
            {
                var props = Ecs.GetSystem<DeviceSystem>().PhysicalDevice!.GetFormatProperties(format);

                switch (tiling)
                {
                    case ImageTiling.Linear when (props.LinearTilingFeatures & features) == features:
                        return format;
                    case ImageTiling.Optimal when (props.OptimalTilingFeatures & features) == features:
                        return format;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(tiling), tiling, "failed to find supported format!");
                }
            }

            throw new ArgumentOutOfRangeException(nameof(candidates), candidates, "failed to find supported format!");
        }

        public void EnsureDepthResourcesExitsA()
        {
            /*
            var depthFormat =;

            ImageComponent.CreateImage(renderEngine.SwapChainComponent.swapChainExtent.Width, renderEngine.SwapChainComponent.swapChainExtent.Height, depthFormat, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachment, MemoryPropertyFlags.DeviceLocal, out depthImage, out depthImageMemory);
            depthImageView = CreateImageView(depthImage, depthFormat, ImageAspectFlags.Depth);

            TransitionImageLayout(depthImage, depthFormat, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);
        */
        }

        public AImage CreateManagedImage(Format format, ImageAspectFlags aspectFlags, Extent2D extent)
        {
            var aImage = CreateImageAndView(extent.Width, extent.Height, format, ImageTiling.Optimal, ImageUsageFlags.DepthStencilAttachment, MemoryPropertyFlags.DeviceLocal, aspectFlags);

            TransitionImageLayout(aImage.Image!, format, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);
            return aImage;
        }

        #region imageHelp

        private static bool HasStencilComponent(Format format)
        {
            return format == Format.D32SFloatS8UInt || format == Format.D24UNormS8UInt;
        }

        public void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
        {
            Ecs.GetSystem<DeviceSystem>().SingleTimeCommand(x => x.GraphicsQueue!, command =>
            {
                command.CopyBufferToImage(buffer, image, ImageLayout.TransferDestinationOptimal, new BufferImageCopy()
                {
                    BufferOffset = 0,
                    BufferRowLength = 0,
                    BufferImageHeight = 0,
                    ImageOffset = new(),
                    ImageExtent = new(width, height, 1),
                    ImageSubresource = new()
                    {
                        AspectMask = ImageAspectFlags.Color,
                        MipLevel = 0,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                    }
                });
            });
        }

        public void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
        {
            ImageSubresourceRange subresourceRange = new()
            {
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            };

            if (newLayout == ImageLayout.DepthStencilAttachmentOptimal)
            {
                subresourceRange.AspectMask = ImageAspectFlags.Depth;

                if (HasStencilComponent(format)) subresourceRange.AspectMask |= ImageAspectFlags.Stencil;
            }
            else
            {
                subresourceRange.AspectMask = ImageAspectFlags.Color;
            }

            var barrier = new ImageMemoryBarrier
            {
                OldLayout = oldLayout,
                NewLayout = newLayout,
                Image = image,
                SubresourceRange = subresourceRange,
            };

            //barrier.SourceQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
            //barrier.DestinationQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;

            PipelineStageFlags sourceStage;
            PipelineStageFlags destinationStage;

            switch (oldLayout)
            {
                case ImageLayout.Undefined when newLayout == ImageLayout.TransferDestinationOptimal:
                    barrier.SourceAccessMask = 0;
                    barrier.DestinationAccessMask = AccessFlags.TransferWrite;

                    sourceStage = PipelineStageFlags.TopOfPipe;
                    destinationStage = PipelineStageFlags.Transfer;
                    break;
                case ImageLayout.TransferDestinationOptimal when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                    barrier.SourceAccessMask = AccessFlags.TransferWrite;
                    barrier.DestinationAccessMask = AccessFlags.ShaderRead;

                    sourceStage = PipelineStageFlags.Transfer;
                    destinationStage = PipelineStageFlags.FragmentShader;
                    break;
                case ImageLayout.Undefined when newLayout == ImageLayout.DepthStencilAttachmentOptimal:
                    barrier.SourceAccessMask = 0;
                    barrier.DestinationAccessMask = AccessFlags.DepthStencilAttachmentRead | AccessFlags.DepthStencilAttachmentWrite;

                    sourceStage = PipelineStageFlags.TopOfPipe;
                    destinationStage = PipelineStageFlags.EarlyFragmentTests;
                    break;
                default:
                    throw new ArgumentException("unsupported layout transition!");
            }

            Ecs.GetSystem<DeviceSystem>().SingleTimeCommand(x => x.GraphicsQueue!, command => command.PipelineBarrier(sourceStage, destinationStage, ArrayProxy<MemoryBarrier>.Null, ArrayProxy<BufferMemoryBarrier>.Null, barrier));
        }

        /// <inheritdoc />
        protected override void Setup()
        {
            Ecs.RegisterInit(this, InitPhase.Init);
            Ecs.GetComponentSystem<AjivaRenderEngine, ARenderAble>().OnResize += (_, _) =>
            {
                //EnsureDepthResourcesDeletion();
                //EnsureDepthResourcesExits();
            };
        }

        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override AImage CreateComponent(IEntity entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
        }

  #endregion

        /// <inheritdoc />
        public void Init(AjivaEcs ecs, InitPhase phase)
        {
            
        }
    }
}
